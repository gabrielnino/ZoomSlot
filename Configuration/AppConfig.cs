using Configuration.Configuration;

namespace Configuration
{
    public class AppConfig
    {
        public LinkedInCredentials LinkedInCredentials { get; set; }
        public BookCredentials BookCredentials { get; set; }
        public JobSearchConfiguration JobSearch { get; set; }
        public Logging Logging { get; set; }

        public LlmProvider Llm { get; set; }

        public PathsConfig Paths { get; set; }
        public ThresholdConfig Thresholds { get; set; }

        public FilePathsConfig FilePaths { get; set; }

        public Gmail Gmail { get; set; }


    }
}