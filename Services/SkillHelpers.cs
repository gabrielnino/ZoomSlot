namespace Services
{
    using System.Text.RegularExpressions;

    public static class SkillHelpers
    {
        public static string CleanSkill(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            var normalizedName = NormalizeSkill(name);
            var withoutNoiseWords = RemoveCommonPrefixes(normalizedName);
            var noUnicode = RemoveUnicodeEscapes(withoutNoiseWords);
            var normalizedSkill = CleanSkillNameLettersAndNumbers(withoutNoiseWords);
            return normalizedSkill;
        }

        public static string CleanSkillNameLettersAndNumbers(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) 
                return string.Empty;
            var alphanumeric = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]", "");
            return alphanumeric;
        }

        public static string RemoveCommonPrefixes(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Convert to lowercase for consistency
            var text = input.ToLowerInvariant();

            // List of words to remove
            string[] noiseWords =
            [
                "experience", "knowledge", "familiarity", "background", " with ", "working", " and ", " in ", " development ", " of ", " proficiency ", " proficient "
            ];

            // Regex to remove each word surrounded by word boundaries
            foreach (var word in noiseWords)
            {
                text = Regex.Replace(text, $@"\b{word}\b", "", RegexOptions.IgnoreCase);
            }

            // Clean up extra spaces and return
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        //experience

        public static string RemoveUnicodeEscapes(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Regex matches \u followed by exactly 4 hexadecimal digits
            return Regex.Replace(input, @"\\u[0-9a-fA-F]{4}", string.Empty);
        }

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
                .Replace("c++", "cpp")
                .Replace("&", " and ")
                .Replace("/", " ")
                .Replace("-", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace("[", " ")
                .Replace("]", " ")
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
                    if (string.IsNullOrWhiteSpace(newCategory))
                    {
                        newCategory = "UNCATEGORIZED";
                    }

                    if (finalGroups.TryGetValue(newCategory, out var list))
                    {
                        list.Add(skill);
                    }
                    else
                    {
                        finalGroups[newCategory] = new List<string> { skill };
                    }
                }
            }

            return finalGroups.ToDictionary(
                g => g.Key,
                g => g.Value.Distinct().OrderBy(s => s).ToList()
            );
        }

    }

}
