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
            if(File.Exists(resumePath))
            {
                File.Delete(resumePath);
            }
            File.Copy(resumeFileName, resumePath);
            await _resolver.InitializeAsync(categoryPath);
            var skills = await _extractor.ExtractSkillsAsync(inputPath);
            var skillsNormalize = skills.Select(SkillHelpers.NormalizeSkill);
            var grouped = _grouper.GroupSkills(skillsNormalize);

            // Add consolidation and reclassification steps
            var consolidated = SkillHelpers.ConsolidateGroups(grouped, _resolver.ResolveCategory);
            var finalGroups = SkillHelpers.ReclassifyGroups(consolidated, _resolver.ResolveCategory);

            await _writer.WriteResultsAsync(finalGroups, outputPath, summaryPath);

            // Process job offers with categories
            var jsonText = await File.ReadAllTextAsync(inputPath);
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
                var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, outputJson);
            }
            else
            {
                _logger.LogWarning("No job offers with valid skills found after normalization.");
            }

            File.Move(_executionOptions.ExecutionFolder, _executionOptions.CompletedFolder);
            RoboCopyFiles(_appConfig.Paths.ReportFolder, _appConfig.Paths.OutPath);
            _logger.LogInformation("🎉 Skill Normalization Completed");
        }


        public static void RoboCopyFiles(string sourceDir, string destDir, string robocopyOptions = "/MOV /NP /R:3 /W:1")
        {
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"Source directory does not exist: {sourceDir}");
                return;
            }

            Directory.CreateDirectory(destDir);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = $"\"{sourceDir}\" \"{destDir}\" {robocopyOptions}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            try
            {
                process.Start();

                // Read output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode > 7)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Robocopy failed with exit code {process.ExitCode}");
                    Console.WriteLine(error);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Robocopy succeeded.");
                    Console.ResetColor();
                    Console.WriteLine(output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running Robocopy: {ex.Message}");
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

            // Process KeySkillsRequired
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

            // Find categories for each skill
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
