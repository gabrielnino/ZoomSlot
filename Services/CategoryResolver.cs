using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Logging;
    using Models;
    using Services.Interfaces;

    public class CategoryResolver : ICategoryResolver
    {
        private readonly ILogger<CategoryResolver> _logger;
        private Dictionary<string, List<string>> _flatCategories = new();
        private readonly IOpenAIClient _openAIClient;

        public CategoryResolver(ILogger<CategoryResolver> logger, IOpenAIClient openAIClient)
        {
            _logger = logger;
            _openAIClient = openAIClient;
        }

        public Dictionary<string, List<string>> FlatCategories => _flatCategories;

        public async Task InitializeAsync(string categoryFilePath)
        {
            _logger.LogInformation("📂 Loading category hierarchy from {Path}", categoryFilePath);

            try
            {
                var json = await File.ReadAllTextAsync(categoryFilePath);
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, options);

                var cleanedData = new Dictionary<string, List<string>>();

                // Loop through each category and clean each skill

                var clasified = data.Keys.Where(k => k != "UNCATEGORIZED");
                foreach (var kvp in data.Where(k => clasified.Contains(k.Key)))
                {
                    var category = kvp.Key;
                    var skills = kvp.Value;

                    var cleanedSkills = skills
                        .Where(s => !string.IsNullOrWhiteSpace(s)) // optional: skip null/empty
                        .Select(SkillHelpers.CleanSkill)
                        .Where(cleaned => !string.IsNullOrWhiteSpace(cleaned)) // skip empty results after cleaning
                        .Distinct()
                        .Order()
                        .ToList();

                    cleanedData[category] = cleanedSkills;
                }

                var uncategorized = data.ContainsKey("UNCATEGORIZED")
                    ? data["UNCATEGORIZED"].Select(SkillHelpers.CleanSkill).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                    : [];




                foreach (var skill in uncategorized)
                {
                    _logger.LogInformation($"📦Generate prompt categorizing with IA: '{skill}'");
                    var prompt = PrompHelpers.GetPrompt(cleanedData, skill);
                    _logger.LogInformation($"📦Categorizing with IA: '{skill}'");
                    var uncategorizeJson = await _openAIClient.GetChatCompletionAsync(prompt);
                    uncategorizeJson = StringHelpers.ExtractJsonContent(uncategorizeJson);
                    var categorization = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(uncategorizeJson, options);
                    foreach(var category in categorization.Keys)
                    {
                        if (!cleanedData.ContainsKey(category))
                        {
                            cleanedData[category] = categorization[category];
                        }
                        cleanedData[category].AddRange(categorization[category].Distinct().Order());
                        _logger.LogInformation($"📦 Reclassified skill '{skill}' into category '{category}'");
                    }
                }


                var jsonResult = JsonSerializer.Serialize(cleanedData, options);
                await File.WriteAllTextAsync(categoryFilePath, jsonResult);


                _flatCategories = cleanedData;

                if (data == null)
                {
                    _logger.LogWarning("⚠️ Category file was read but returned null after deserialization.");
                    _flatCategories = [];
                }
                else
                {
                    _flatCategories = data;
                    _logger.LogInformation("✅ Loaded {CategoryCount} categories", _flatCategories.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load or parse the category hierarchy file: {Path}", categoryFilePath);
                _flatCategories = [];
            }
        }

        public string ResolveCategory(string skill)
        {
            var norm = SkillHelpers.CleanSkill(skill);
            string foundCategory = "GENERAL_TECH";
            bool matched = false;

            foreach (var (cat, keywords) in _flatCategories)
            {
                if (keywords.Any(k => norm.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    foundCategory = cat;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                _logger.LogDebug("Uncategorized skill: {Skill} (normalized: {Normalized})", skill, norm);
            }

            return foundCategory;
        }


        public async Task WriteAsync(string categoryFilePath, List<string> uncategorized)
        {
            _flatCategories.Add("UNCATEGORIZED", [.. uncategorized.Distinct().Order()]);
            _logger.LogInformation("💾 Writing category hierarchy to {Path}", categoryFilePath);

            try
            {
                var json = JsonSerializer.Serialize(_flatCategories, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(categoryFilePath, json);
                _logger.LogInformation("✅ Successfully saved {CategoryCount} categories to file.", _flatCategories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to write category hierarchy to file: {Path}", categoryFilePath);
            }
        }
    }

}
