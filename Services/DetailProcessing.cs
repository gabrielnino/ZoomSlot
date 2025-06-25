using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;
using System.Text.Json;

namespace Services
{
    public class DetailProcessing : IDetailProcessing
    {
        private readonly ILogger<DetailProcessing> _logger;
        private readonly IWebDriver _driver;
        private readonly List<JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private readonly ILoginService _loginService;
        private readonly IJobStorageService _jobStorageService;
        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers.json");
        private string OffersDetailFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers_detail.json");

        private List<string> _offersPending;

        public DetailProcessing(
            IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ILoginService loginService,
            IJobStorageService jobStorageService)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _offersDetail = [];
            _capture = capture;
            _executionOptions = executionOptions;
            _offersPending = LoadPendingOffers(OffersFilePath);
            _loginService = loginService;
            _jobStorageService = jobStorageService;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
        {
            if (_offersPending != null && _offersPending.Any())
            {
                offers = _offersPending;
                _logger.LogInformation($"📂 ID:{_executionOptions.TimeStamp} Resuming processing from existing offers.json with {offers.Count()} pending offers.");
            }
            _logger.LogInformation($"🔐 ID:{_executionOptions.TimeStamp} Logging into LinkedIn...");
            await _loginService.LoginAsync();
            foreach (var offer in offers.ToList())
            {
                try
                {
                    _logger.LogInformation($"🌐 ID:{_executionOptions.TimeStamp} Navigating to job offer URL: {offer}");
                    _driver.Navigate().GoToUrl(offer);
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "BeforeExtraction");
                    var detail = await ExtractDetailAsync(searchText);
                    _offersDetail.Add(detail);
                    _offersPending.Remove(offer);
                    await _jobStorageService.SaveOffersAsync(OffersFilePath, _offersPending);
                    await _jobStorageService.SaveJobOfferDetailAsync(OffersDetailFilePath, _offersDetail);
                    _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully processed and saved job offer: {offer}");
                }
                catch (WebDriverException ex)
                {
                    _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} WebDriver error occurred while processing offer: {offer}");
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "WebDriverError");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Unexpected error occurred while processing offer: {offer}");
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "GeneralError");
                }
            }
            _logger.LogInformation($"📦 ID:{_executionOptions.TimeStamp} Finished processing. Total offers processed: {_offersDetail.Count}, remaining: {_offersPending.Count}");
            return _offersDetail;
        }


        private async Task<JobOfferDetail> ExtractDetailAsync(string searchText)
        {
            var titleElement = _driver.FindElement(By.CssSelector("h1.t-24.t-bold.inline"));
            var companyElement = _driver.FindElement(By.CssSelector(".job-details-jobs-unified-top-card__company-name a"));
            var descriptionElement = _driver.FindElement(By.CssSelector("article.jobs-description__container"));
            return new JobOfferDetail
            {
                ID = Guid.NewGuid().ToString(),
                JobOfferTitle = titleElement?.Text.Trim() ?? string.Empty,
                CompanyName = companyElement?.Text.Trim() ?? string.Empty,
                ContactHiringSection = "",
                Applicants = "",
                Description = descriptionElement?.Text.Trim() ?? string.Empty,
                SalaryOrBudgetOffered = "",
                Link = _driver.Url,
                SearchText = searchText
            };
        }

        public List<string> LoadPendingOffers(string offersFilePath)
        {
            try
            {
                if (File.Exists(offersFilePath))
                {
                    var json = File.ReadAllText(offersFilePath);
                    var urls = JsonSerializer.Deserialize<List<string>>(json);
                    _logger.LogInformation("Loaded {Count} pending offers from {Path}", urls?.Count ?? 0, offersFilePath);
                    return urls ?? [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending offers from file");
            }

            return [];
        }
    }
}
