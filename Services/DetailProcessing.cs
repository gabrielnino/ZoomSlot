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
        private readonly WebDriverWait _wait;
        private readonly List<JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private readonly ILoginService _loginService;
        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers.json");
        private string OffersDetailFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers_detail.json");

        private List<string> _offersPending;

        public DetailProcessing(
            IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ILoginService loginService)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(90));
            _offersDetail = [];
            _capture = capture;
            _executionOptions = executionOptions;
            _offersPending = LoadPendingOffers();
            _loginService = loginService;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
        {
            if (_offersPending != null && _offersPending.Any())
            {
                offers = _offersPending;
               _logger.LogInformation($"💼 ID:{_executionOptions.TimeStamp} Loaded {offers.Count()}.");
            }

            await _loginService.LoginAsync(); // Ensure login before processing offers
            foreach (var offer in offers)
            {
                try
                {
                    _logger.LogInformation("Navigating to job offer URL: {Url}", offer);
                    _driver.Navigate().GoToUrl(offer);
                    await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "BeforeExtraction");
                    var detail = await ExtractDetailAsync(searchText);
                    _offersDetail.Add(detail);
                    _offersPending.Remove(offer);
                    await SavePendingOffersAsync();
                    await SaveOffersDetailAsync();
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

        private List<string> LoadPendingOffers()
        {
            try
            {
                if (File.Exists(OffersFilePath))
                {
                    var json = File.ReadAllText(OffersFilePath);
                    var urls = JsonSerializer.Deserialize<List<string>>(json);
                    _logger.LogInformation("Loaded {Count} pending offers from {Path}", urls?.Count ?? 0, OffersFilePath);
                    return urls ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending offers from file");
            }

            return new List<string>();
        }

        private async Task SavePendingOffersAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_offersPending, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(OffersFilePath, json);
                _logger.LogInformation("Updated offers.json with {Count} pending URLs", _offersPending.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save pending offers to file");
            }
        }

        private async Task SaveOffersDetailAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_offersDetail, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(OffersDetailFilePath, json);
                _logger.LogInformation("Saved offers_detail.json with {Count} processed offers", _offersDetail.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save offers detail to file");
            }
        }
    }
}
