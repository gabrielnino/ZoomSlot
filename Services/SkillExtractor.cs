namespace Services
{
    using System.Text.Json.Nodes;
    using Microsoft.Extensions.Logging;
    using Services.Interfaces;

    public class SkillExtractor(ILogger<SkillExtractor> logger) : ISkillExtractor
    {
        private readonly ILogger<SkillExtractor> _logger = logger;

        public async Task<List<string>> ExtractSkillsAsync(string inputFilePath)
        {
            _logger.LogInformation("🔍 Extracting skills from file: {InputFile}", inputFilePath);

            var text = await File.ReadAllTextAsync(inputFilePath);
            var root = JsonNode.Parse(text)?.AsArray();

            var skills = new HashSet<string>();
            foreach (var job in root ?? [])
            {
                foreach (var key in new[] { "KeySkillsRequired" })
                {
                    foreach (var skill in job?[key]?.AsArray() ?? [])
                    {
                        var name = skill?["Name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var cleaned = SkillHelpers.CleanSkill(name);
                            if (!SkillHelpers.ShouldDiscard(cleaned))
                                skills.Add(cleaned);
                        }
                    }
                }
            }

            _logger.LogInformation("✅ Extracted {SkillCount} unique skills", skills.Count);
            return [.. skills];
        }
    }

}
