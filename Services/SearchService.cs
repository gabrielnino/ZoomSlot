using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;

namespace Services
{
    public class SearchService : ISearchService
    {
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<SearchService> _logger;
        private readonly ExecutionOptions _executionOptions;
        private readonly ICaptureService _capture;
        private const string FolderName = "Search";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly ISecurityCheckHelper _securityCheck;

        public SearchService(IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<SearchService> logger,
            ICaptureService capture,
            ExecutionOptions executionOptions)
        {
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;
            _executionOptions = executionOptions;
            _logger.LogInformation($"📁 Created execution folder at: {_executionOptions.ExecutionFolder}");
            _capture = capture;

        }

        public async Task PerformSearchAsync()
        {
            _logger.LogInformation("🔍 Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(FolderPath, "JobsPageLoaded");

            var searchInput = _driver.FindElements(By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]"))
                                     .FirstOrDefault();
            if (searchInput == null)
            {
                throw new InvalidOperationException($"❌ Job search input field not found. Current URL: {_driver.Url}");
            }

            _logger.LogInformation($"🔎 Executing job search with keyword: '{_config.JobSearch.SearchText}'...");
            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(FolderPath, "SearchExecuted");
            _logger.LogInformation($"✅ Search executed for: '{_config.JobSearch.SearchText}'.");
        }
    }
}
