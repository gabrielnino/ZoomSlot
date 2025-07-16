using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services.Interfaces;

namespace Services
{
    public class SkillNormalizerService(
        ICategoryResolver resolver,
        IResultWriter writer,
        AppConfig appConfig,
        ILogger<SkillNormalizerService> logger,
        ExecutionOptions executionOptions) : ISkillNormalizerService
    {
        private readonly ICategoryResolver _resolver = resolver;
        private readonly IResultWriter _writer = writer;
        private readonly AppConfig _appConfig = appConfig;
        private readonly ILogger<SkillNormalizerService> _logger = logger;
        private readonly ExecutionOptions _executionOptions = executionOptions;
        private readonly List<string>  Uncategorized = [];

        public async Task RunAsync()
        {
            _logger.LogInformation("🚀 Skill Normalization Process Started");

            string inputPath = _appConfig.Paths.InputFile;
            string categoryPath = _appConfig.Paths.CategoryFile;
            string outputPath = _appConfig.Paths.NormalizedOutputFile;
            string summaryPath = _appConfig.Paths.SummaryFile;
            string resumeFileName = _appConfig.Paths.ResumeFile;
            var resumePath = Path.Combine(_appConfig.Paths.OutPath, resumeFileName);

            _logger.LogInformation("📁 Preparing resume file...");
             File.Copy(resumeFileName, resumePath, overwrite: true);
            _logger.LogInformation("📖 Initializing category resolver...");
            await _resolver.InitializeAsync(categoryPath);
            _logger.LogInformation("🔍 Extracting skills from input file...");
            var filePath = Path.Combine(_executionOptions.ExecutionFolder, _appConfig.Paths.InputFile);
            _logger.LogInformation("📦 Processing job offers...");
            var jsonText = await File.ReadAllTextAsync(filePath);
            var jobOffers = JsonSerializer.Deserialize<List<JobOffer>>(jsonText) ?? [];
            var result = new List<JobOffer>();

            foreach (var jobOffer in jobOffers)
            {
                var categories = ExtractCategories(jobOffer);
                if (categories.Count > 0)
                {
                    jobOffer.Skills = categories;
                    result.Add(jobOffer);
                }
            }

            if (result.Count > 0)
            {
                _logger.LogInformation("📤 Writing categorized job offers...");
                var skillsData = GetSkillsKeysWithCount(result);
                var skillsDictionary = skillsData.ToDictionary( x => x.Key, x => x.Count);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(skillsDictionary, options);
                File.WriteAllText(resumePath, json);

                var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, outputJson);
            }
            else
            {
                _logger.LogWarning("⚠️ No job offers with valid skills found after normalization.");
            }

            _logger.LogInformation("📁 Moving execution folder to completed folder...");
            _logger.LogInformation("📁 Copying reports with Robocopy...");
            RoboCopyFiles(_appConfig.Paths.ReportFolder, _appConfig.Paths.OutPath);
            _logger.LogInformation("✅ Skill Normalization Completed Successfully");
        }

        public static List<(string Key, int Count)> GetSkillsKeysWithCount(List<JobOffer> jobOffers)
        {
            var keyCounter = new Dictionary<string, int>();

            foreach (var offer in jobOffers)
            {
                var keys = offer?.Skills?.Keys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
                if (keys == null || keys.Count == 0)
                {
                    continue;
                }

                foreach (var key in keys)
                {
                    keyCounter[key] = keyCounter.GetValueOrDefault(key) + 1;
                }
            }

            return [.. keyCounter
                .OrderByDescending(kv => kv.Value)
                .Select(kv => (kv.Key, kv.Value))
                .Take(40)];
        }

        public void RoboCopyFiles(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);

                foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, file);
                    string destPath = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(file, destPath, overwrite: true);
                }

                _logger.LogInformation("📁 Files copied from execution to completed folder.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to copy folder content from {sourceDir} to {destDir}");
                throw;
            }
        }

        private Dictionary<string, List<Skill>> ExtractCategories(JobOffer offer)
        {

            var skillCategories = new Dictionary<string, List<Skill>>();
            var skillRelevanceLookup = new Dictionary<string, Skill>();

            if (offer.KeySkillsRequired != null)
            {
                foreach (var skill in offer.KeySkillsRequired)
                {
                    if (skill != null && !string.IsNullOrWhiteSpace(skill.Name))
                    {
                        var normalized = SkillHelpers.CleanSkill(skill.Name);
                        skill.NormailizeName = normalized;
                        skillRelevanceLookup[normalized] = skill;
                    }
                }
            }

            foreach (var skill in skillRelevanceLookup)
            {
                var category = _resolver.FindBestCategory(skill.Value.NormailizeName);
                if (skillCategories.ContainsKey(category.Key))
                {
                    skillCategories[category.Key].Add(skill.Value);
                    continue;
                }

                skillCategories.Add(category.Key, [skill.Value]);

            }

            return skillCategories;
        }
    }
}
