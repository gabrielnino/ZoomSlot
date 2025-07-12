using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    using System.Text.RegularExpressions;
    using System.Text.Json;

    public static class SkillHelpers
    {
        public static string CleanSkillName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var skill = name.ToLowerInvariant().Trim();
            skill = Regex.Replace(skill, @"\([^)]*\)", "");
            skill = Regex.Replace(skill, @"\[[^\]]*\]", "");
            skill = Regex.Replace(skill, @"^proficiency in\s*", "");
            return skill.Trim('.', ',', ';', ':', ' ');
        }
        private static readonly string[] sourceArray = [
            "years of experience", "experience with",
            "knowledge of", "understanding of"
        ];

        public static bool ShouldDiscard(string skill)
        {
            if (skill.Length < 2 || skill.Split().Length > 6) return true;
            if (Regex.IsMatch(skill, @"\d+\+?\s*years?")) return true;
            return sourceArray.Any(p => skill.Contains(p));
        }

        public static string NormalizeSkill(string skill)
        {
            return skill.ToLowerInvariant()
                .Replace("vb.net", "vbnet")
                .Replace(".net", "dotnet")
                .Replace("c#", "csharp")
                .Replace("&", " and ")
                .Replace("/", " ")
                .Replace("-", " ")
                .Replace("  ", " ")
                .Trim();
        }

        public static bool ShouldGroupTogether(string a, string b, double threshold)
        {
            var na = NormalizeSkill(a);
            var nb = NormalizeSkill(b);
            if (na == nb) return true;
            if (na[..Math.Min(4, na.Length)] == nb[..Math.Min(4, nb.Length)]
                && na.Length <= 5 && nb.Length <= 5) return true;

            return Similarity(na, nb) >= threshold;
        }

        public static double Similarity(string s1, string s2)
        {
            int matches = s1.Zip(s2, (a, b) => a == b ? 1 : 0).Sum();
            return 2.0 * matches / (s1.Length + s2.Length);
        }

        public static Dictionary<string, List<string>> FlattenCategories(Dictionary<string, object> root, string prefix = "")
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var (key, value) in root)
            {
                if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
                {
                    result[prefix + key] = je.EnumerateArray().Select(x => x.GetString()!).ToList();
                }
                else if (value is JsonElement je2 && je2.ValueKind == JsonValueKind.Object)
                {
                    var nested = JsonSerializer.Deserialize<Dictionary<string, object>>(je2.ToString()!)!;
                    foreach (var kv in FlattenCategories(nested, $"{prefix}{key}_"))
                        result[kv.Key] = kv.Value;
                }
            }
            return result;
        }

        public static Dictionary<string, List<string>> ConsolidateGroups(
            Dictionary<string, List<string>> groups, 
            Func<string, string> categoryResolver, 
            int minGroupSize = 3)
        {
            var consolidated = new Dictionary<string, List<string>>();

            foreach (var group in groups)
            {
                var skills = group.Value;
                var category = categoryResolver(group.Key);

                if (skills.Count < minGroupSize)
                {
                    if (consolidated.ContainsKey(category))
                    {
                        consolidated[category].AddRange(skills);
                    }
                    else
                    {
                        consolidated[category] = new List<string>(skills);
                    }
                }
                else
                {
                    consolidated[group.Key] = skills;
                }
            }

            return consolidated;
        }

        public static Dictionary<string, List<string>> ReclassifyGroups(
            Dictionary<string, List<string>> groups, 
            Func<string, string> categoryResolver)
        {
            var finalGroups = new Dictionary<string, List<string>>();

            foreach (var group in groups)
            {
                var category = group.Key;
                var skills = group.Value;

                foreach (var skill in skills)
                {
                    if (ShouldDiscard(skill)) continue;

                    var newCategory = categoryResolver(skill);

                    if (finalGroups.TryGetValue(newCategory, out List<string>? value))
                    {
                        value.Add(skill);
                    }
                    else
                    {
                        finalGroups[newCategory] = new List<string> { skill };
                    }
                }
            }

            // Remove duplicates and sort
            return finalGroups.ToDictionary(
                g => g.Key,
                g => g.Value.Distinct().OrderBy(s => s).ToList()
            );
        }
    }

}
