using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class LinkedInService : ILinkedInService, IDisposable
    {
        private const string CssSelectorToFind = "input[placeholder='Describe the job you want']";
        private const string Message = "Error during job search";
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<LinkedInService> _logger;
        private readonly IJobStorageService _storageService;
        private readonly bool _debugMode;
        private readonly string _executionFolder;

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

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _executionFolder = Path.Combine(Directory.GetCurrentDirectory(), $"Execution_{timestamp}");
            Directory.CreateDirectory(_executionFolder);
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
                var (htmlPath, screenshotPath) = await TakeScreenshot(Message, true);
                _logger.LogError($"Search input not found. Page HTML saved to {htmlPath}, screenshot to {screenshotPath}");
                throw;
            }
        }

        private bool IsOnLoginPage()
        {
            var usernameElements = _driver.FindElements(By.Id("username"));
            var passwordElements = _driver.FindElements(By.Id("password"));
            var urlContainsLogin = _driver.Url.Contains("/login");
            return usernameElements.Count > 0 && passwordElements.Count > 0 && urlContainsLogin;
        }

        private bool IsSecurityCheckPresent()
        {
            var securityCheckHeader = _driver.FindElements(By.XPath("//h1[contains(text(), 'Let's do a quick security check')]"));
            var startPuzzleButton = _driver.FindElements(By.XPath("//button[contains(text(), 'Start Puzzle')]"));

            return securityCheckHeader.Count > 0 || startPuzzleButton.Count > 0;
        }

        private async Task LoginAsync()
        {

            _driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            await Task.Delay(3000);
            if (!IsOnLoginPage())
            {
                if (IsSecurityCheckPresent())
                {
                    if (_debugMode)
                    {
                        await HandleSecurityCheckInDebugMode();
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "LinkedIn requires manual security check. Please login manually in a browser first.");
                    }
                }
                if (_debugMode)
                {
                    await HandleUnexpectedPage();
                }
                throw new InvalidOperationException("Failed to load LinkedIn login page");
            }

            var emailInput = _driver.FindElement(By.Id("username"));
            emailInput.SendKeys(_config.LinkedInCredentials.Email);
            _ = TakeScreenshot("Set the email");
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            _ = TakeScreenshot("Set the password");
            await Task.Delay(3000);
            _logger.LogInformation("Successfully logged in to LinkedIn");

        }


        private async Task HandleUnexpectedPage()
        {
            var (htmlPath, screenshotPath) = await TakeScreenshot("UnexpectedPage");
            _logger.LogError($"Unexpected page loaded. HTML: {htmlPath}, Screenshot: {screenshotPath}");

            Console.WriteLine("=====================================");
            Console.WriteLine("DEBUG MODE: Unexpected page detected");
            Console.WriteLine($"Check debug files at:");
            Console.WriteLine($"- HTML: {htmlPath}");
            Console.WriteLine($"- Screenshot: {screenshotPath}");
            Console.WriteLine("=====================================");
        }

        private async Task HandleSecurityCheckInDebugMode()
        {
            _logger.LogWarning("Security check detected - waiting for manual completion in debug mode");

            var (htmlPath, screenshotPath) = await TakeScreenshot("Security Check");
            _logger.LogInformation($"Security check debug files saved: {htmlPath}, {screenshotPath}");

            Console.WriteLine("=============================================");
            Console.WriteLine("LinkedIn requires a manual security check.");
            Console.WriteLine("Please complete the puzzle in the browser window.");
            Console.WriteLine("Press ENTER when done to continue...");
            Console.WriteLine("=============================================");

            Console.ReadLine();

            // Wait additional time after manual intervention
            await Task.Delay(5000);
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
                throw new InvalidOperationException($"Search input element not found.");
            }

            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            _logger.LogInformation($"Search performed for: {_config.JobSearch.SearchText}");
        }

        private async Task<(string htmlPath, string screenshotPath)> TakeScreenshot(string stage = null, bool isError=false)
        {
            if (!_debugMode)
            {
                return (null, null);
            }
            var (html, screenshot) = await TakeScreenshot(isError);
            if(!isError)
            {
                _logger.LogInformation($"{stage} debug: {html}, {screenshot}");
            }
            return (html, screenshot);
        }

        private async Task<(string htmlPath, string screenshotPath)> TakeScreenshot(bool isError)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var subfolder = isError ? "Errors" : "Debug";
            // Create subfolder structure: Execution_{timestamp}/subfolder/stageFolder/
            var fullPath = Path.Combine(_executionFolder, subfolder);
            Directory.CreateDirectory(fullPath);
            var htmlPath = Path.Combine(fullPath, $"LinkedInPage_{timestamp}.html");
            var screenshotPath = Path.Combine(fullPath, $"LinkedInPage_{timestamp}.png");
            var pageSource = _driver.PageSource;
            await File.WriteAllTextAsync(htmlPath, pageSource);
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
