using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;

namespace Services
{
    public class JobSearch : IJobSearch
    {
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<JobSearch> _logger;
        private readonly ExecutionOptions _executionOptions;
        private readonly ICaptureSnapshot _capture;
        private readonly ISecurityCheck _securityCheck;
        private const string FolderName = "Search";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);

        public JobSearch(IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<JobSearch> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ISecurityCheck securityCheck)
        {
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;
            _executionOptions = executionOptions;
            _logger.LogInformation($"📁 Created execution folder at: {_executionOptions.ExecutionFolder}");
            _capture = capture;
            _securityCheck = securityCheck;

        }

        public async Task PerformSearchAsync()
        {
            _logger.LogInformation("🔍 Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");
            await Task.Delay(3000);

            // Check for security verification or unexpected pages before proceeding
            if (_securityCheck.IsSecurityChek())
            {
                await _securityCheck.HandleSecurityPage();
                throw new InvalidOperationException(
                    "❌ LinkedIn requires manual security verification. Please complete verification in the browser before proceeding.");
            }

            // No need to call HandleUnexpectedPage if IsSecurityChek is true
            // Instead, check if we're on the correct page by looking for expected elements
            var searchInput = _driver.FindElements(By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]"))
                                     .FirstOrDefault();

            if (searchInput == null)
            {
                await _securityCheck.HandleUnexpectedPage();
                throw new InvalidOperationException(
                    $"❌ Job search input field not found. Possibly unexpected page. Current URL: {_driver.Url}");
            }

            await _capture.CaptureArtifacts(FolderPath, "JobsPageLoaded");

            _logger.LogInformation($"🔎 Executing job search with keyword: '{_config.JobSearch.SearchText}'...");
            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);

            await _capture.CaptureArtifacts(FolderPath, "SearchExecuted");
            _logger.LogInformation($"✅ Search executed for: '{_config.JobSearch.SearchText}'.");
        }
    }
}
