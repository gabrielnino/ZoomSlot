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
    public class SkillGrouper : ISkillGrouper
    {
        private readonly ILogger<SkillGrouper> _logger;
        private readonly AppConfig _appConfig;

        public SkillGrouper(ILogger<SkillGrouper> logger, AppConfig appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        public Dictionary<string, List<string>> GroupSkills(IEnumerable<string> skills)
        {
            double threshold = _appConfig.Thresholds.Similarity;
            var groups = new Dictionary<string, List<string>>();

            foreach (var skill in skills)
            {
                var match = groups.Keys.FirstOrDefault(k => SkillHelpers.ShouldGroupTogether(skill, k, threshold));
                if (match != null)
                    groups[match].Add(skill);
                else
                    groups[skill] = new List<string> { skill };
            }

            _logger.LogInformation("📦 Grouped {GroupCount} skills into clusters", groups.Count);
            return groups;
        }
    }

}
