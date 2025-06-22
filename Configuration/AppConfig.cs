namespace Configuration
{
    public class AppConfig
    {
        public LinkedInCredentials LinkedInCredentials { get; set; }
        public JobSearchConfiguration JobSearch { get; set; }
        public Logging Logging { get; set; }

        public LlmProvider Llm { get; set; }
    }
}
