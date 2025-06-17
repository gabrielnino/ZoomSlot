using System.Diagnostics;
using Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace Services
{
    public class LinkedInService : ILinkedInService, IDisposable
    {
        private const string ErrorMessage = "Job search operation failed";
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<LinkedInService> _logger;
        private readonly bool _debugMode;
        private readonly string _executionFolder;
        private bool _disposed = false;

        public LinkedInService(
            IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<LinkedInService> logger,
            CommandArgs commandArgs)
        {
            _debugMode = commandArgs.IsDebugMode;
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _executionFolder = Path.Combine(Directory.GetCurrentDirectory(), $"Execution_{timestamp}");
            Directory.CreateDirectory(_executionFolder);

            _logger.LogInformation($"📁 Created execution folder at: {_executionFolder}");
        }

        public async Task SearchJobsAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Starting LinkedIn job search process");
                await LoginAsync();
                await PerformSearchAsync();
                await ProcessAllPagesAsync();
                _logger.LogInformation("✅ Job search completed successfully");
            }
            catch (Exception ex)
            {
                var (htmlPath, screenshotPath) = await TakeScreenshot(ErrorMessage, true);
                _logger.LogError(ex, $"❌ Critical error during job search. Debug info saved to:\nHTML: {htmlPath}\nScreenshot: {screenshotPath}");
                throw new ApplicationException("Job search failed. See inner exception for details.", ex);
            }
            finally
            {
                Dispose();
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
            _logger.LogInformation("🔐 Attempting to login to LinkedIn...");
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
                            "LinkedIn requires manual security verification. Please login manually in browser first.");
                    }
                }

                if (_debugMode)
                {
                    await HandleUnexpectedPage();
                }

                throw new InvalidOperationException(
                    $"Failed to load LinkedIn login page. Current URL: {_driver.Url}");
            }

            var emailInput = _driver.FindElement(By.Id("username"));
            emailInput.SendKeys(_config.LinkedInCredentials.Email);
            _ = TakeScreenshot("Entered email");

            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            _ = TakeScreenshot("Entered password");

            await Task.Delay(3000);
            _logger.LogInformation("✅ Successfully authenticated with LinkedIn");
        }

        private async Task HandleUnexpectedPage()
        {
            var (htmlPath, screenshotPath) = await TakeScreenshot("UnexpectedPageDetected");

            _logger.LogError($"Unexpected page layout detected. Debug info saved to:\nHTML: {htmlPath}\nScreenshot: {screenshotPath}");

            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║           UNEXPECTED PAGE DETECTED          ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine($"║ Current URL: {_driver.Url,-30} ║");
            Console.WriteLine("║                                            ║");
            Console.WriteLine($"║ HTML saved to: {htmlPath,-25} ║");
            Console.WriteLine($"║ Screenshot saved to: {screenshotPath,-18} ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");
        }

        private async Task HandleSecurityCheckInDebugMode()
        {
            var (htmlPath, screenshotPath) = await TakeScreenshot("SecurityVerification");

            _logger.LogWarning($"⚠️ Security verification required. Debug info saved to:\nHTML: {htmlPath}\nScreenshot: {screenshotPath}");

            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║         SECURITY VERIFICATION REQUIRED       ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine("║ LinkedIn requires additional verification: ║");
            Console.WriteLine("║ 1. Complete the security check in browser  ║");
            Console.WriteLine("║ 2. Press ENTER to continue automation      ║");
            Console.WriteLine("║                                            ║");
            Console.WriteLine($"║ Debug files saved to:                     ║");
            Console.WriteLine($"║ - HTML: {htmlPath,-30} ║");
            Console.WriteLine($"║ - Screenshot: {screenshotPath,-23} ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");

            Console.ReadLine();
            await Task.Delay(5000);
            _logger.LogInformation("🔄 Resuming automation after security check");
        }

        private async Task PerformSearchAsync()
        {
            _logger.LogInformation("🔍 Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");
            await Task.Delay(3000);

            _ = TakeScreenshot("JobsPageLoaded");

            var search = By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]");
            var searchInput = _driver.FindElements(search).FirstOrDefault();

            if (searchInput == null)
            {
                throw new InvalidOperationException(
                    $"Job search input not found on page. Current URL: {_driver.Url}");
            }

            _logger.LogInformation($"🔎 Searching for: '{_config.JobSearch.SearchText}'");
            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);

            await Task.Delay(3000);
            _ = TakeScreenshot("SearchExecuted");

            ScrollMove();
            await Task.Delay(3000);

            _logger.LogInformation($"✅ Search completed for: '{_config.JobSearch.SearchText}'");
        }

        private async Task<(string htmlPath, string screenshotPath)> TakeScreenshot(string? stage = null, bool isError = false)
        {
            if (!_debugMode) return (null, null);

            var (html, screenshot) = await CaptureDebugArtifacts(isError);

            if (!isError && stage != null)
            {
                _logger.LogDebug($"📸 Debug capture for '{stage}':\nHTML: {html}\nScreenshot: {screenshot}");
            }

            return (html, screenshot);
        }

        private async Task<(string htmlPath, string screenshotPath)> CaptureDebugArtifacts(bool isError)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var subfolder = isError ? "Errors" : "Debug";
            var fullPath = Path.Combine(_executionFolder, subfolder);

            Directory.CreateDirectory(fullPath);

            var htmlPath = Path.Combine(fullPath, $"Page_{timestamp}.html");
            var screenshotPath = Path.Combine(fullPath, $"Screenshot_{timestamp}.png");

            await File.WriteAllTextAsync(htmlPath, _driver.PageSource);
            ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile(screenshotPath);

            return (htmlPath, screenshotPath);
        }

        private async Task ProcessAllPagesAsync()
        {
            int pageCount = 0;
            var offers = new List<string>();

            _logger.LogInformation($"📄 Processing up to {_config.JobSearch.MaxPages} pages of results");

            do
            {
                pageCount++;
                _logger.LogInformation($"📖 Processing page {pageCount}...");

                var pageOffers = await GetCurrentPageOffersAsync();
                offers.AddRange(pageOffers);

                _logger.LogInformation($"✔️ Page {pageCount} processed. Found {pageOffers?.Count() ?? 0} listings");

                if (pageCount >= _config.JobSearch.MaxPages)
                {
                    _logger.LogInformation($"ℹ️ Reached maximum page limit of {_config.JobSearch.MaxPages}");
                    break;
                }

            } while (await NavigateToNextPageAsync());

            _logger.LogInformation($"🎉 Completed processing. Total {offers.Count} opportunities found across {pageCount} pages");
        }

        public void ScrollMove()
        {
            var xpathSearchResults = "//ul[contains(@class, 'semantic-search-results-list')]";
            var scrollable = _driver.FindElements(By.XPath(xpathSearchResults)).FirstOrDefault();

            if (scrollable == null)
            {
                _logger.LogWarning("⚠️ Scroll container not found - skipping scroll operation");
                return;
            }

            var jsExecutor = (IJavaScriptExecutor)_driver;
            long scrollHeight = (long)jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollable);
            long currentPosition = 0;

            _logger.LogDebug($"🖱️ Beginning scroll through results (height: {scrollHeight}px)");

            while (currentPosition < scrollHeight)
            {
                currentPosition += 10;
                jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[1];", scrollable, currentPosition);
                Thread.Sleep(50);
            }

            _logger.LogDebug("🖱️ Finished scrolling to bottom of results");
        }

        public async Task<IEnumerable<string>?> GetCurrentPageOffersAsync()
        {
            await Task.Delay(2000);

            var jobContainer = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]"))
                                    .FirstOrDefault();

            if (jobContainer == null)
            {
                _logger.LogWarning("⚠️ Job listings container not found on page");
                return null;
            }

            var offers = new List<string>();
            var jobNodes = jobContainer.FindElements(By.XPath(".//li[contains(@class, 'semantic-search-results-list__list-item')]"));

            if (jobNodes == null || !jobNodes.Any())
            {
                _logger.LogWarning("⚠️ No job listings found on current page");
                return null;
            }

            _logger.LogDebug($"🔍 Found {jobNodes.Count} job listings on page");

            foreach (var jobNode in jobNodes)
            {
                try
                {
                    var jobUrl = ExtractJobUrl(jobNode);
                    if (!string.IsNullOrEmpty(jobUrl))
                    {
                        offers.Add(jobUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ Failed to process job listing {jobNode.GetAttribute("id")}");
                }
            }

            return offers;
        }

        private string? ExtractJobUrl(IWebElement jobNode)
        {
            var card = jobNode.FindElements(By.XPath(".//div[contains(@class, 'job-card-job-posting-card-wrapper')]"))
                             .FirstOrDefault()
                      ?? jobNode.FindElements(By.XPath(".//div[contains(@class, 'semantic-search-results-list__list-item')]"))
                             .FirstOrDefault();

            if (card == null)
            {
                throw new Exception($"Job card element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobAnchor = card.FindElements(By.CssSelector("a.job-card-job-posting-card-wrapper__card-link"))
                               .FirstOrDefault();

            if (jobAnchor == null)
            {
                throw new Exception($"Job link element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobUrl = jobAnchor.GetAttribute("href");
            if (string.IsNullOrEmpty(jobUrl))
            {
                throw new Exception($"Empty URL in listing {jobNode.GetAttribute("id")}");
            }

            return jobUrl;
        }

        public async Task<bool> NavigateToNextPageAsync()
        {
            try
            {
                var nextButton = _driver.FindElements(By.CssSelector("button[aria-label='Next']"))
                                      .FirstOrDefault(b => b.Enabled);

                if (nextButton == null)
                {
                    _logger.LogInformation("⏹️ No more pages available - reached end of results");
                    return false;
                }

                _logger.LogDebug("⏭️ Attempting to navigate to next page");
                nextButton.Click();
                await Task.Delay(3000);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error while attempting to navigate to next page");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger.LogDebug("🧹 Cleaning up browser resources...");
                _driver?.Quit();
                _driver?.Dispose();
                _logger.LogInformation("✅ Resources cleaned up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred during cleanup");
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~LinkedInService()
        {
            Dispose();
        }
    }
}