using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class DetailProcessing : IDetailProcessing
    {
        private readonly ILogger<DetailProcessing> _logger;
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly List<JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;

        public DetailProcessing(
            IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(90));
            _offersDetail = new List<JobOfferDetail>();
            _capture = capture;
            _executionOptions = executionOptions;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
        {
            foreach (var offer in offers)
            {
                try
                {
                    _logger.LogInformation("Navigating to job offer URL: {Url}", offer);
                    _driver.Navigate().GoToUrl(offer);

                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "BeforeExtraction");

                    var detail = await ExtractDetailAsync(searchText);
                    _offersDetail.Add(detail);

                    _logger.LogInformation("Successfully processed job offer: {Url}", offer);
                }
                catch (WebDriverException ex)
                {
                    _logger.LogError(ex, "WebDriver error on URL: {Url}", offer);
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "WebDriverError");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error on URL: {Url}", offer);
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "GeneralError");
                }
            }

            return _offersDetail;
        }

        private async Task<JobOfferDetail> ExtractDetailAsync(string searchText)
        {
            // Example of valid selectors
            var titleElement = _driver.FindElement(By.CssSelector("h1.t-24.t-bold.inline"));
            var companyElement = _driver.FindElement(By.CssSelector(".job-details-jobs-unified-top-card__company-name a"));
            var descriptionElement = _driver.FindElement(By.CssSelector("article.jobs-description__container"));

            return new JobOfferDetail
            {
                ID = Guid.NewGuid().ToString(),
                JobOfferTitle = titleElement?.Text.Trim() ?? string.Empty,
                CompanyName = companyElement?.Text.Trim() ?? string.Empty,
                ContactHiringSection = "", // Implement extraction if needed
                Applicants = "", // Implement extraction if needed
                Description = descriptionElement?.Text.Trim() ?? string.Empty,
                SalaryOrBudgetOffered = "", // Implement extraction if needed
                Link = _driver.Url,
                SearchText = searchText
            };
        }
    }
}
