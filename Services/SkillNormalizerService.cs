using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    using System.Configuration;
    using Configuration;
    using Microsoft.Extensions.Logging;
    using Services.Interfaces;

    public class SkillNormalizerService : ISkillNormalizerService
    {
        private readonly ISkillExtractor _extractor;
        private readonly ISkillGrouper _grouper;
        private readonly ICategoryResolver _resolver;
        private readonly IResultWriter _writer;
        private readonly AppConfig _appConfig;
        private readonly ILogger<SkillNormalizerService> _logger;

        public SkillNormalizerService(
            ISkillExtractor extractor,
            ISkillGrouper grouper,
            ICategoryResolver resolver,
            IResultWriter writer,
            AppConfig appConfig,
            ILogger<SkillNormalizerService> logger)
        {
            _extractor = extractor;
            _grouper = grouper;
            _resolver = resolver;
            _writer = writer;
            _appConfig = appConfig;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("🚀 Skill Normalization Process Started");

            string inputPath = _appConfig.Paths.InputFile;
            string categoryPath = _appConfig.Paths.CategoryFile;
            string outputPath = _appConfig.Paths.NormalizedOutputFile;
            string summaryPath = _appConfig.Paths.SummaryFile;

            await _resolver.InitializeAsync(categoryPath);
            var skills = await _extractor.ExtractSkillsAsync(inputPath);
            var grouped = _grouper.GroupSkills(skills);

            var finalGroups = grouped
                .GroupBy(kv => _resolver.ResolveCategory(kv.Key))
                .ToDictionary(g => g.Key, g => g.SelectMany(x => x.Value).Distinct().OrderBy(x => x).ToList());

            await _writer.WriteResultsAsync(finalGroups, outputPath, summaryPath);

            _logger.LogInformation("🎉 Skill Normalization Completed");
        }
    }

}
