namespace Models
{
    public class ExecutionOptions
    {
        public ExecutionOptions()
        {
            TimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string ExecutionFolder => Path.Combine(Directory.GetCurrentDirectory(), $"{FolderName}_{TimeStamp}");
        public static string FolderName => "Execution";
        public string TimeStamp { get; }

        public int MaxParallelism => Environment.ProcessorCount / 2;
    }
}
