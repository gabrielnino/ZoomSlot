using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;

namespace Services
{
    public class LinkedInService : ILinkedInService, IDisposable
    {
        private const string CssSelectorToFind = "input[placeholder='Describe the job you want']";
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<LinkedInService> _logger;
        private readonly IJobStorageService _storageService;
        private readonly bool _debugMode;

        public LinkedInService(
            IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<LinkedInService> logger,
            IJobStorageService storageService,
            CommandArgs commandArgs)
        {
            _debugMode = commandArgs.IsDebugMode;
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;
            _storageService = storageService;
        }

        public async Task SearchJobsAsync()
        {
            try
            {
                await LoginAsync();
                await PerformSearchAsync();
                await ProcessAllPagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job search");
                throw;
            }
        }

        private async Task LoginAsync()
        {
            _driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            var emailInput = _driver.FindElement(By.Id("username"));
            emailInput.SendKeys(_config.LinkedInCredentials.Email);
            TakeShop("Set the email");
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            TakeShop("Set the password");
            await Task.Delay(3000); // Wait for login to complete
            _logger.LogInformation("Successfully logged in to LinkedIn");
        }

        private async Task PerformSearchAsync()
        {
            _logger.LogInformation("Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");

            // Wait for page to load
            await Task.Delay(3000);

            // Save HTML for debugging if element not found
            var searchInput = _driver.FindElements(By.CssSelector(CssSelectorToFind))
                                    .FirstOrDefault();

            if (searchInput == null)
            {
                (string htmlPath, string screenshotPath) = await TakeShop();
                _logger.LogError($"Search input not found. Page HTML saved to {htmlPath}, screenshot to {screenshotPath}");
                throw new InvalidOperationException($"Search input element not found. Debug files saved: {htmlPath}, {screenshotPath}");
            }

            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            _logger.LogInformation($"Search performed for: {_config.JobSearch.SearchText}");
        }

        private async Task TakeShop(string stage)
        {
            if(!_debugMode)
            {
                return;
            }
            try
            {
                var (html, screenshot) = await TakeShop();
                _logger.LogInformation($"{stage} debug: {html}, {screenshot}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{stage} debug failed");
            }
        }

        private async Task<(string htmlPath, string screenshotPath)> TakeShop()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseFolder = Path.Combine(Directory.GetCurrentDirectory(), timestamp);
            Directory.CreateDirectory(baseFolder);
            var htmlPath = Path.Combine(baseFolder, $"LinkedInPage_{timestamp}.html");
            var screenshotPath = Path.Combine(baseFolder, $"LinkedInPage_{timestamp}.png");

            // Save HTML
            var pageSource = _driver.PageSource;
            await File.WriteAllTextAsync(htmlPath, pageSource);

            // Save screenshot
            ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile(screenshotPath);
            return (htmlPath, screenshotPath);
        }

        private async Task ProcessAllPagesAsync()
        {
            int pageCount = 0;
            var offers = new List<JobOffer>();

            do
            {
                pageCount++;
                _logger.LogInformation($"Processing page {pageCount}");

                var pageOffers = await GetCurrentPageOffersAsync();
                offers.AddRange(pageOffers);

                if (pageCount >= _config.JobSearch.MaxPages)
                    break;

            } while (await NavigateToNextPageAsync());

            await _storageService.SaveJobsAsync(offers);
            _logger.LogInformation($"Saved {offers.Count} job offers to storage");
        }

        public async Task<IEnumerable<JobOffer>> GetCurrentPageOffersAsync()
        {
            await Task.Delay(2000); // Wait for page to load

            var jobElements = _driver.FindElements(By.CssSelector(".jobs-search-results__list-item"));
            var offers = new List<JobOffer>();

            foreach (var jobElement in jobElements)
            {
                try
                {
                    var offer = new JobOffer
                    {
                        Title = jobElement.FindElement(By.CssSelector(".job-card-list__title")).Text,
                        Company = jobElement.FindElement(By.CssSelector(".job-card-container__company-name")).Text,
                        Location = jobElement.FindElement(By.CssSelector(".job-card-container__metadata-item")).Text,
                        Url = jobElement.FindElement(By.CssSelector(".job-card-list__title")).GetAttribute("href")
                    };

                    offers.Add(offer);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing job offer");
                }
            }

            return offers;
        }

        public async Task<bool> NavigateToNextPageAsync()
        {
            try
            {
                var nextButton = _driver.FindElements(By.CssSelector("button[aria-label='Next']"))
                    .FirstOrDefault(b => b.Enabled);

                if (nextButton == null)
                    return false;

                nextButton.Click();
                await Task.Delay(3000); // Wait for next page to load
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error navigating to next page");
                return false;
            }
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
