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
    }

}
