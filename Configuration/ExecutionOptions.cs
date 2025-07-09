namespace Configuration
{
    public class ExecutionOptions
    {
        private readonly string _outPath;
        public ExecutionOptions(string outPath)
        {
            _outPath = outPath;
            TimeStamp = ActiveTimeStamp ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string ExecutionFolder => Path.Combine(_outPath, $"{FolderName}_{TimeStamp}");
        public string CompletedFolder => Path.Combine(_outPath, $"{CompletedFolderName}_{TimeStamp}");
        public static string FolderName => "Execution";
        public static string CompletedFolderName => "Completed";
        public string TimeStamp { get; }
        private string? ActiveTimeStamp
        {
            get
            {
                var current = _outPath;
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
