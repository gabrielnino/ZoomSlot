namespace Commands
{
    public class JobCommandArgs
    {
        public const string search = "--search";
        public const string detail = "--detail";
        public const string export = "--export";
        public const string job = "--job";
        public const string apply = "--apply";
        public const string prompt = "--prompt";
        public const string skills = "--skills";
        public const string resume = "--resume";
        public const string book = "--book";

        private static readonly HashSet<string> ValidCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            search,
            detail,
            export,
            job,
            apply,
            prompt,
            skills,
            resume,
            book
        };

        public string MainCommand { get; }
        public Dictionary<string, string> Arguments { get; }

        public JobCommandArgs(string[] args)
        {
            MainCommand = args.FirstOrDefault(IsCommand) ?? args.FirstOrDefault(IsArgument).Split("=").FirstOrDefault();
            Arguments = args
                .Where(IsArgument)
                .Select(arg =>
                {
                    var parts = arg.Split('=', 2);
                    var key = parts[0];
                    var value = parts.Length > 1 ? parts[1] : string.Empty;
                    return new KeyValuePair<string, string>(key, value);
                })
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsCommand(string arg) => ValidCommands.Contains(arg);

        private static bool IsArgument(string arg) => arg.Contains("=");
    }
}
