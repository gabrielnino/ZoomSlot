namespace Services
{
    public class JobCommandArgs
    {
        private static readonly HashSet<string> ValidCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "--search",
            "--export",
            "--job",
            "--apply",
            "--prompt",
            "--qualified",
            "--skill"
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
