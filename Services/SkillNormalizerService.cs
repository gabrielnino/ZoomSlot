using System;
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

        public async Task RunAsync()
        {
            _logger.LogInformation("🚀 Skill Normalization Process Started");

            string inputPath = _appConfig.Paths.InputFile;
            string categoryPath = _appConfig.Paths.CategoryFile;
            string outputPath = _appConfig.Paths.NormalizedOutputFile;
            string summaryPath = _appConfig.Paths.SummaryFile;
            await _resolver.InitializeAsync(categoryPath);
            var skills = await _extractor.ExtractSkillsAsync(inputPath);
            var skillsNormalize = skills.Select(x=>SkillHelpers.NormalizeSkill(x));
            var grouped = _grouper.GroupSkills(skillsNormalize);

            var finalGroups = grouped
                .GroupBy(kv => _resolver.ResolveCategory(kv.Key))
                .ToDictionary(g => g.Key, g => g.SelectMany(x => x.Value).Distinct().OrderBy(x => x).ToList());

            await _writer.WriteResultsAsync(finalGroups, outputPath, summaryPath);

            var jsonText = await File.ReadAllTextAsync(inputPath);
            var jobOffers = JsonSerializer.Deserialize<List<JobOffer>>(jsonText) ?? [];
            var resutl = new List<JobOffer>();
            foreach (var jobOffer in jobOffers)
            {
                var categories = ExtractCategories(jobOffer, finalGroups);
                var skillsCategories = categories.Select(c => new SkillCategory { Category = c.Category, Relevance = c.Relevance }).ToList();
                if (categories.Count > 0)
                {
                    jobOffer.Skills = skillsCategories;
                    resutl.Add(jobOffer);
                }
            }

            if (resutl.Count > 0)
            {
                var outputJson = JsonSerializer.Serialize(resutl, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, outputJson);
            }
            else
            {
                _logger.LogWarning("No job offers with valid skills found after normalization.");
            }
            _logger.LogInformation("🎉 Skill Normalization Completed");
        }


        private static List<SkillCategory>? ExtractCategories(JobOffer offer, Dictionary<string, List<string>> finalGroups)
        {
            if (offer == null || finalGroups == null)
            {
                return [];
            }
            var keySkills = offer.KeySkillsRequired.Select(
                k => new Skill() 
                { 
                    Name = SkillHelpers.NormalizeSkill(k.Name), 
                    RelevancePercentage = k.RelevancePercentage
                }
                ) ?? [];


            //var skillRelevanceLookup = new[] { offer.KeySkillsRequired }
            //.Where(skills => skills != null)
            //.SelectMany(skills => skills)
            //.ToDictionary(skill => SkillHelpers.NormalizeSkill(skill.Name), skill => skill.RelevancePercentage);

            // var categoryRelevance = skillRelevanceLookup
            //     .Select(skill => new SkillCategory() { Relevance = skill.Value })
            //    Category: finalGroups.FirstOrDefault(g => g.Value.Contains(skill.Key)).Key,
            //    Relevance: skill.Value
            //)).ToList();

            return null;// categoryRelevance;
        }
    }

}
