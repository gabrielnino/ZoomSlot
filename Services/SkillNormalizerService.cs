using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class SkillNormalizerService(
        ISkillExtractor extractor,
        ISkillGrouper grouper,
        ICategoryResolver resolver,
        IResultWriter writer,
        AppConfig appConfig,
        ILogger<SkillNormalizerService> logger,
        ExecutionOptions executionOptions) : ISkillNormalizerService
    {
        private readonly ISkillExtractor _extractor = extractor;
        private readonly ISkillGrouper _grouper = grouper;
        private readonly ICategoryResolver _resolver = resolver;
        private readonly IResultWriter _writer = writer;
        private readonly AppConfig _appConfig = appConfig;
        private readonly ILogger<SkillNormalizerService> _logger = logger;
        private readonly ExecutionOptions _executionOptions = executionOptions;

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
            _logger.LogInformation("📁 Resume file copied and overwritten if existed.");

            _logger.LogInformation("📖 Initializing category resolver...");
            await _resolver.InitializeAsync(categoryPath);

            _logger.LogInformation("🔍 Extracting skills from input file...");

            var filePath = Path.Combine(_executionOptions.ExecutionFolder, _appConfig.Paths.InputFile);

            var skills = await _extractor.ExtractSkillsAsync(filePath);

            _logger.LogInformation("🧽 Normalizing skills...");
            var skillsNormalize = skills.Select(SkillHelpers.NormalizeSkill);

            _logger.LogInformation("🧠 Grouping skills...");
            var grouped = _grouper.GroupSkills(skillsNormalize);

            _logger.LogInformation("🔗 Consolidating groups...");
            var consolidated = SkillHelpers.ConsolidateGroups(grouped, _resolver.ResolveCategory);

            _logger.LogInformation("🧮 Reclassifying groups...");
            var finalGroups = SkillHelpers.ReclassifyGroups(consolidated, _resolver.ResolveCategory);

            _logger.LogInformation("💾 Writing results to output and summary files...");
            await _writer.WriteResultsAsync(finalGroups, outputPath, summaryPath);

            _logger.LogInformation("📦 Processing job offers...");
            var jsonText = await File.ReadAllTextAsync(filePath);
            var jobOffers = JsonSerializer.Deserialize<List<JobOffer>>(jsonText) ?? [];
            var result = new List<JobOffer>();

            foreach (var jobOffer in jobOffers)
            {
                var categories = ExtractCategories(jobOffer, finalGroups);
                if (categories.Count > 0)
                {
                    jobOffer.Skills = categories;
                    result.Add(jobOffer);
                }
            }

            if (result.Count > 0)
            {
                _logger.LogInformation("📤 Writing categorized job offers...");
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

        private static List<SkillCategory> ExtractCategories(JobOffer offer, Dictionary<string, List<string>> finalGroups)
        {
            if (offer == null || finalGroups == null)
            {
                return [];
            }

            var skillCategories = new List<SkillCategory>();
            var skillRelevanceLookup = new Dictionary<string, int>();

            if (offer.KeySkillsRequired != null)
            {
                foreach (var skill in offer.KeySkillsRequired)
                {
                    if (skill != null && !string.IsNullOrWhiteSpace(skill.Name))
                    {
                        var normalized = SkillHelpers.NormalizeSkill(skill.Name);
                        skillRelevanceLookup[normalized] = skill.RelevancePercentage;
                    }
                }
            }

            foreach (var skill in skillRelevanceLookup)
            {
                var category = finalGroups.FirstOrDefault(g => g.Value.Contains(skill.Key));
                if (category.Key != null)
                {
                    skillCategories.Add(new SkillCategory
                    {
                        Category = category.Key,
                        Relevance = skill.Value
                    });
                }
            }

            return skillCategories;
        }
    }
}
