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
        public string CompletedFolder => Path.Combine(_outPath, CompletedActiveTimeStamp);
        public static string FolderName => "Execution";
        public static string CompletedFolderName => "Completed";
        public string TimeStamp { get; }
        private string? ActiveTimeStamp
        {
            get
            {
                return GetCurrentFolder(FolderName);
            }
        }

        private string CompletedActiveTimeStamp
        {
            get
            {
                string? folder = GetCurrentFolder(CompletedFolderName);
                var suffix = folder ?? TimeStamp;
                return $"{CompletedFolderName}_{suffix}";
            }
        }

        public string? GetCurrentFolder(string folder)
        {
            var current = _outPath;
            var pattern = $"{folder}_*";
            var directories = Directory.GetDirectories(current, $"{folder}_*");

            var lastDirectory = directories
                .OrderByDescending(dir => dir)
                .FirstOrDefault();

            if (lastDirectory == null)
            {
                return null;
            }

            var folderName = Path.GetFileName(lastDirectory);

            if (folderName != null && folderName.StartsWith($"{folder}_"))
            {
                return folderName.Substring(folder.Length + 1); // +1 for underscore
            }

            return null;
        }
    }
}
