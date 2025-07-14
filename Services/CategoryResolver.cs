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
    using Services.Interfaces;

    public class CategoryResolver : ICategoryResolver
    {
        private readonly ILogger<CategoryResolver> _logger;
        private Dictionary<string, List<string>> _flatCategories = new();

        public CategoryResolver(ILogger<CategoryResolver> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, List<string>> FlatCategories => _flatCategories;

        public async Task InitializeAsync(string categoryFilePath)
        {
            _logger.LogInformation("📂 Loading category hierarchy from {Path}", categoryFilePath);

            try
            {
                var json = await File.ReadAllTextAsync(categoryFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
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
            var norm = SkillHelpers.NormalizeSkill(skill);

            foreach (var (cat, keywords) in _flatCategories)
            {
                if (keywords.Any(k => Regex.IsMatch(norm, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase)))
                    return cat;
            }

            return "GENERAL_TECH";
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
