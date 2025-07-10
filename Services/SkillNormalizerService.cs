using Configuration;
using Microsoft.Extensions.Logging;
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
            var grouped = _grouper.GroupSkills(skills);

            var finalGroups = grouped
                .GroupBy(kv => _resolver.ResolveCategory(kv.Key))
                .ToDictionary(g => g.Key, g => g.SelectMany(x => x.Value).Distinct().OrderBy(x => x).ToList());

            await _writer.WriteResultsAsync(finalGroups, outputPath, summaryPath);

            _logger.LogInformation("🎉 Skill Normalization Completed");
        }
    }

}
