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

        public async Task InitializeAsync(string categoryFilePath)
        {
            _logger.LogInformation("📂 Loading category hierarchy from {Path}", categoryFilePath);

            var json = await File.ReadAllTextAsync(categoryFilePath);
            var root = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            _flatCategories = SkillHelpers.FlattenCategories(root);
            _logger.LogInformation("✅ Loaded {CategoryCount} categories", _flatCategories.Count);
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
    }

}
