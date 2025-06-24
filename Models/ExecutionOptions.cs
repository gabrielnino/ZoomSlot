namespace Models
{
    public class ExecutionOptions
    {
        public ExecutionOptions()
        {
            TimeStamp = ActiveTimeStamp ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string ExecutionFolder => Path.Combine(Directory.GetCurrentDirectory(), $"{FolderName}_{TimeStamp}");
        public static string FolderName => "Execution";
        public string TimeStamp { get; }

        public int MaxParallelism => Math.Max(1, Environment.ProcessorCount / 2);

        private string? ActiveTimeStamp
        {
            get
            {
                var current = Directory.GetCurrentDirectory();
                var pattern = $"{FolderName}_*";
                var directories = Directory.GetDirectories(current, $"{FolderName}_*");

                var lastDirectory = directories
                    .OrderByDescending(dir => dir)
                    .FirstOrDefault();

                if (lastDirectory == null)
                {
                    return null;
                }

                var folderName = Path.GetFileName(lastDirectory);

                if (folderName != null && folderName.StartsWith($"{FolderName}_"))
                {
                    return folderName.Substring(FolderName.Length + 1); // +1 for underscore
                }

                return null;
            }
        }
    }
}
