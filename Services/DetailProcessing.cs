using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class DetailProcessing : IDetailProcessing
    {
        private readonly ILogger<DetailProcessing> _logger;
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly List<Models.JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Detail";
        private readonly ISecurityCheck _securityCheck;
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;

        public DetailProcessing(IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ISecurityCheck securityCheck,
            ExecutionOptions executionOptions,
            IDirectoryCheck directoryCheck)
        {
            _offersDetail = [];
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _capture = capture;
            _securityCheck = securityCheck;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers)
        {
            _logger.LogInformation($"📝 ID:{_executionOptions.TimeStamp} Processing detailed job offer data...");

            foreach (var offer in offers)
            {
                try
                {
                    _logger.LogDebug($"🌐 ID:{_executionOptions.TimeStamp} Navigating to job offer URL: {offer}");
                    _driver.Navigate().GoToUrl(offer);
                    _wait.Until(driver =>
                    {
                        var xPathJobs = "//div[contains(@class, 'jobs-box--with-cta-large')]";
                        var el = driver.FindElements(By.XPath(xPathJobs)).FirstOrDefault();
                        return el != null && el.Displayed;
                    });
                    if (_securityCheck.IsSecurityChek())
                    {
                        await _securityCheck.TryStartPuzzle();
                    }

                    await _capture.CaptureArtifacts(FolderPath, "Detailed Job offer");

                    var offersDetail = await ExtractDescriptionLinkedIn();
                    if (offersDetail != null)
                    {
                        _offersDetail.Add(offersDetail);
                        _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Detailed job offer processed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Failed to process detailed job offer at URL: {offer}");
                    await _capture.CaptureArtifacts(FolderPath, "Error in Detailed Job Offer");
                    // Continue with next offer instead of stopping
                }
            }
            return _offersDetail;
        }
        public async Task<Models.JobOfferDetail> ExtractDescriptionLinkedIn()
        {
            _logger.LogDebug($"🔍 ID:{_executionOptions.TimeStamp} Extracting job details from current page...");

            await _capture.CaptureArtifacts(FolderPath, "Extract description");

            var details = _driver.FindElements(By.XPath("//div[contains(@class, 'jobs-box--with-cta-large')]"));
            if (!details.Any())
            {
                var message = $"❌ Job details container not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            var detail = details.FirstOrDefault(x => x != null);
            _logger.LogDebug($"✅ ID:{_executionOptions.TimeStamp} Job details container found.");

            var seeMoreButtons = detail.FindElements(By.XPath("//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]"));
            if (!seeMoreButtons.Any())
            {
                var message = $"❌ ID:{_executionOptions.TimeStamp} 'See more' button not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            _logger.LogDebug($"✅ ID:{_executionOptions.TimeStamp} 'See more' button found.");

            var seeMoreButton = seeMoreButtons.FirstOrDefault(x => x != null);
            seeMoreButton.Click();

            await _capture.CaptureArtifacts(FolderPath, "ExtractDescriptionLinkedIn");

            var jobOffer = new Models.JobOfferDetail();
            return jobOffer;
        }
    }
}
