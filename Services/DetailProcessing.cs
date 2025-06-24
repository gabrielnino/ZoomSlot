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
        private const string FolderName = "Detail";
        private readonly ISecurityCheck _securityCheck;
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;
        private readonly IUtil _util;

        public DetailProcessing(IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ISecurityCheck securityCheck,
            ExecutionOptions executionOptions,
            IDirectoryCheck directoryCheck,
            IUtil util)
        {
            _offersDetail = [];
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(90));
            _capture = capture;
            _securityCheck = securityCheck;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _util = util;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
        {
            _logger.LogInformation($"📝 ID:{_executionOptions.TimeStamp} Processing detailed job offer data...");

            foreach (var offer in offers)
            {
                int retryCount = 0;
                bool success = false;
                while (retryCount < 3 && !success) 
                {
                    try
                    {
                        _logger.LogDebug($"🌐 ID:{_executionOptions.TimeStamp} Navigating to job offer URL: {offer} (Attempt {retryCount + 1})");
                        _driver.Navigate().GoToUrl(offer);
                        _wait.Until(driver =>
                        {
                            var xPathJobs = "//div[contains(@class, 'jobs-box--with-cta-large')]";
                            var el = driver.FindElements(By.XPath(xPathJobs)).FirstOrDefault();
                            return el != null && el.Displayed;
                        });
                        if (_securityCheck.IsSecurityCheck())
                        {
                            await _securityCheck.HandleSecurityPage();
                        }
                        await _capture.CaptureArtifactsAsync(FolderPath, "Detailed Job offer");
                        var offersDetail = await ExtractDetail(searchText);
                        if (offersDetail != null)
                        {
                            offersDetail.SearchText = searchText;
                            _offersDetail.Add(offersDetail);
                            _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Detailed job offer processed successfully.");
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= 3)
                        {
                            _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Failed to process detailed job offer at URL: {offer}");
                            await _capture.CaptureArtifactsAsync(FolderPath, $"Error_Attempt_{retryCount}");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} Retrying ({retryCount}/3) for URL: {offer}");
                            await Task.Delay(2000 * retryCount);  // Exponential backoff
                        }
                    }
                    if(retryCount != 0)
                    {
                        await Task.Delay(3000 * retryCount + new Random().Next(1000, 3000));
                    }
                }
            }
            return _offersDetail;
        }

        public async Task<JobOfferDetail> ExtractDetail(string searchText)
        {
            _logger.LogDebug($"🔍 ID:{_executionOptions.TimeStamp} Extracting job details from current page...");
            await _capture.CaptureArtifactsAsync(FolderPath, "ExtractDescription_Start");
            var detail = ExtractDetail();
            await _capture.CaptureArtifactsAsync(FolderPath, "ExtractDescription_AfterSeeMore");
            var header = ExtractHeader();
            var jobOfferTitle = ExtractTitle(header);
            var companyName = ExtractCompany(header);
            var contactHiringSection = ExtractContactHiring(detail);
            var applicants = ExtractApplicants(detail);
            var descriptionText = ExtractDescription(detail);
            var salaryOrBudgetOffered = ExtractSalary(detail);
            var url = _driver.Url;
            var id = _util.ExtractJobId(url);
            if (id == null)
            {
                throw new ArgumentException($"Invalid url: {url} does not have a valid ID");
            }
            return new JobOfferDetail
            {
                ID = id,
                JobOfferTitle = jobOfferTitle,
                CompanyName = companyName,
                ContactHiringSection = contactHiringSection,
                Applicants = applicants,
                Description = descriptionText,
                SalaryOrBudgetOffered = salaryOrBudgetOffered,
                Link = url,
                SearchText = searchText
            };
        }

        private IWebElement ExtractHeader()
        {
            var header = _driver.FindElements(By.XPath("//div[contains(@class, 't-14') and contains(@class, 'artdeco-card')]")).FirstOrDefault();
            if (header == null)
            {
                var message = $"❌ ID:{_executionOptions.TimeStamp} Header not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            return header;
        }

        private IWebElement ExtractDetail()
        {
            var details = _driver.FindElements(By.XPath("//div[contains(@class, 'jobs-box--with-cta-large')]"));
            if (details.Count == 0)
            {
                var message = $"❌ Job details container not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }
            var detail = details.First();
            ClickSeeMore(detail);
            return detail;
        }

        private void ClickSeeMore(IWebElement detail)
        {
            try
            {
                // Try to expand description
                var seeMoreButton = detail.FindElements(By.XPath(".//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]")).FirstOrDefault();
                if (seeMoreButton != null)
                {
                    seeMoreButton.Click();
                    _logger.LogDebug($"✅ ID:{_executionOptions.TimeStamp} 'See more' button clicked.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} Could not click 'See more' button: {ex.Message}");
            }
        }

        private string ExtractApplicants(IWebElement detail)
        {
            var applicants = _wait.Until(driver => driver.FindElements(By.XPath("//div[contains(@class, 'job-details-jobs-unified-top-card__primary-description-container')]")));
            return applicants.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
        private static string ExtractSalary(IWebElement detail)
        {
            IEnumerable<IWebElement> jobDetailsContainers(IWebElement scope) => scope.FindElements(By.CssSelector(".artdeco-card.job-details-module"));
            return jobDetailsContainers(detail)
                .SelectMany(c => c.FindElements(By.XPath(".//p[contains(., 'CA$')]")))
                .FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
        private static string ExtractDescription(IWebElement detail)
        {

            // Description
            return detail.FindElements(By.CssSelector("article.jobs-description__container")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractContactHiring(IWebElement detail)
        {

            // Hiring team
            return detail.FindElements(By.CssSelector("div.job-details-module .jobs-poster__name strong")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractCompany(IWebElement header)
        {
            return header.FindElements(By.CssSelector(".job-details-jobs-unified-top-card__company-name a")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractTitle(IWebElement header)
        {
            return header.FindElements(By.CssSelector("h1.t-24.t-bold.inline")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
    }
}
