namespace Services
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class StringHelpers
    {
        private static readonly HashSet<string> CommonCountries = new(
        [
            "canada", "united states", "brazil", "mexico", "argentina", "united kingdom",
            "france", "germany", "italy", "spain", "australia", "new zealand", "japan",
            "china", "india", "russia", "south africa", "egypt", "nigeria", "kenya",
            "sweden", "norway", "finland", "denmark", "portugal", "netherlands",
            "belgium", "switzerland", "austria", "greece", "turkey", "saudi arabia",
            "uae", "south korea", "vietnam", "thailand", "philippines", "indonesia",
            "malaysia", "singapore", "israel", "pakistan", "bangladesh", "iran",
            "iraq", "syria", "afghanistan", "ukraine", "poland", "czech republic",
            "hungary", "slovakia", "romania", "bulgaria", "serbia", "croatia",
            "bosnia", "slovenia", "albania", "macedonia", "montenegro", "iceland",
            "ireland", "scotland", "wales", "england", "cuba", "jamaica", "haiti",
            "dominican republic", "colombia", "venezuela", "chile", "peru", "bolivia",
            "ecuador", "paraguay", "uruguay", "guyana", "suriname", "belize",
            "guatemala", "honduras", "el salvador", "costa rica", "panama"
        ],
        StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ExcludeWords = new(
        [
            "with",
            "and",
            "&",
            "the",
            "a",
            "an"
        ],
        StringComparer.OrdinalIgnoreCase);


        private static readonly HashSet<string> CommonTechKeywords = new(
        [
            "csharp", "dotnet", "angular", "react", "vue", "java", "python",
            "javascript", "typescript", "sql", "nosql", "aws", "azure",
            "devops", "cloud", "engineer", "developer", "architect", "manager",
            "consultant", "data", "scientist", "machine", "learning", "ai",
            "software", "fullstack", "backend", "frontend", "qa", "tester",
            "mobile", "android", "ios"
        ],
        StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> ReplacementTerms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "C#", "CSharp" },
            { ".NET", "DotNet" },
            { "++", "PlusPlus" },
            { "+", "Plus" },
            {"FULLSTACK", "Fullstack" }
        };

        public static string ExtractJsonContent(string input)
        {
            string startMarker = "```json";
            string endMarker = "```";
            if (!input.Contains(startMarker) && !input.Contains(endMarker))
                return input;
            int startIndex = input.IndexOf(startMarker);
            if (startIndex == -1)
                return input.Trim();
            startIndex += startMarker.Length;
            int endIndex = input.IndexOf(endMarker, startIndex);
            if (endIndex == -1)
                endIndex = input.Length;
            string jsonContent = input.Substring(startIndex, endIndex - startIndex).Trim();
            return jsonContent;
        }

        public static string NormalizeCompanyName(string companyName)
        {
            return Normalize(companyName, CommonTechKeywords, ReplacementTerms);
        }

        public static string NormalizeLocationName(string locationName)
        {
            return Normalize(locationName, CommonCountries);
        }

        public static string NormalizeJobKeywords(string jobKeywords)
        {
            return Normalize(jobKeywords, CommonTechKeywords, ReplacementTerms);
        }

        public static string NormalizeJobTitle(string jobTitle)
        {
            return Normalize(jobTitle, CommonTechKeywords, ReplacementTerms, ExcludeWords);
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalizedText = text.Normalize(NormalizationForm.FormD);
            return new string([.. normalizedText.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
        }

        private static string Normalize(string textToNormalize, HashSet<string> recognizedTerms, Dictionary<string, string>? replacementTerms = null, HashSet<string>? excludeWords = null)
        {
            if (string.IsNullOrWhiteSpace(textToNormalize))
                return string.Empty;

            var normalizedPattern = @"^([A-Z][a-z0-9]*)(-[A-Z][a-z0-9]*)*$";
            if (Regex.IsMatch(textToNormalize, normalizedPattern))
                return textToNormalize;

            textToNormalize = RemoveDiacritics(textToNormalize);
            textToNormalize = ApplyTermReplacements(textToNormalize, replacementTerms);

            var words = Regex.Split(textToNormalize, @"[^A-Za-z0-9]+")
            .Where(word => !string.IsNullOrEmpty(word) && IsExcludedWord(excludeWords, word))
            .Select(w => NormalizeWord(w, recognizedTerms))
            .ToList();

            var normalizedText = string.Join("-", words);
            return normalizedText.Length > 50 ? normalizedText[..50] : normalizedText;
        }

        private static string ApplyTermReplacements(string textToNormalize, Dictionary<string, string>? replacementTerms)
        {
            if (replacementTerms != null)
            {
                foreach (var term in replacementTerms)
                {
                    textToNormalize = Regex.Replace(textToNormalize, Regex.Escape(term.Key), term.Value, RegexOptions.IgnoreCase);
                }
            }

            return textToNormalize;
        }

        private static string NormalizeWord(string word, HashSet<string> recognizedTerms)
        {
            var lowerWord = word.ToLowerInvariant();
            var matchedTerm = recognizedTerms.FirstOrDefault(term => lowerWord.Contains(term));
            if (matchedTerm != null)
            {
                string normalizedTerm = CapitalizeFirstLetter(matchedTerm);
                bool isExactMatch = matchedTerm.Length == word.Length;
                var replacement = isExactMatch ? normalizedTerm : $"-{normalizedTerm}";
                word = word.Replace(matchedTerm, replacement);
            }

            return CapitalizeFirstLetter(word);
        }

        private static bool IsExcludedWord(HashSet<string>? excludeWords, string word)
        {
            return excludeWords is null || !excludeWords.Contains(word);
        }

        private static string CapitalizeFirstLetter(string word) =>
            string.IsNullOrEmpty(word) ? word : char.ToUpper(word[0]) + word[1..].ToLower();
    }
}
