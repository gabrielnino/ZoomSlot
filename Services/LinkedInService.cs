using System.Diagnostics;
using Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V135.DOM;

namespace Services
{
    public class LinkedInService : ILinkedInService, IDisposable
    {
        private const string Message = "Error during job search";
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<LinkedInService> _logger;
        private readonly bool _debugMode;
        private readonly string _executionFolder;

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
        }

        public async Task SearchJobsAsync()
        {
            try
            {
                //await CloseChromeAsync();
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
            _ = TakeScreenshot("Go jobs");
            // Save HTML for debugging if element not found
            var search = By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]");
            var searchInput = _driver.FindElements(search)
                                    .FirstOrDefault();
            //
            if (searchInput == null)
            {
                throw new InvalidOperationException($"Search input element not found.");
            }

            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            _ = TakeScreenshot("Search jobs");
            ScrollMove();
            await Task.Delay(3000);
            _ = TakeScreenshot("Move scroll");
            _logger.LogInformation($"Search performed for: {_config.JobSearch.SearchText}");
          
        }

        private async Task<(string htmlPath, string screenshotPath)> TakeScreenshot(string? stage = null, bool isError=false)
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
            var offers = new List<string>();

            do
            {
                pageCount++;
                _logger.LogInformation($"Processing page {pageCount}");

                var pageOffers = await GetCurrentPageOffersAsync();
                offers.AddRange(pageOffers);

                if (pageCount >= _config.JobSearch.MaxPages)
                    break;

            } while (await NavigateToNextPageAsync());

            _logger.LogInformation($"Saved {offers.Count} job offers to storage");
        }

        public void ScrollMove()
        {
            var xpathSearchResults = "//ul[contains(@class, 'semantic-search-results-list')]";
            var bySearchResult = By.XPath(xpathSearchResults);
            var scrollables = _driver.FindElements(bySearchResult);

            if (scrollables == null || scrollables.Count() == 0)
            {
                return;
            }

            var scrollable = scrollables.FirstOrDefault();
            var jsExecutor = (IJavaScriptExecutor)_driver;

            // Get the total scroll height
            long scrollHeight = (long)jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollable);
            long currentPosition = 0;

            // Scroll in 10-pixel increments
            while (currentPosition < scrollHeight)
            {
                currentPosition += 10;
                jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[1];", scrollable, currentPosition);

                // Optional: Add a small delay between scroll steps
                System.Threading.Thread.Sleep(50);
            }

            // Ensure we reach exactly the bottom
            jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", scrollable);
        }

        private async Task CloseChromeAsync()
        {
            var tasks = new List<Task>();

            foreach (var process in Process.GetProcessesByName("chrome"))
            {
                tasks.Add(CloseProcessAsync(process));
            }

            await Task.WhenAll(tasks);
        }

        private async Task CloseProcessAsync(Process process)
        {
            if (process.HasExited) return;

            try
            {
                if (process.CloseMainWindow())
                {
                    // Wait for exit asynchronously with timeout
                    var exitTask = WaitForExitAsync(process);
                    var timeoutTask = Task.Delay(2000);

                    if (await Task.WhenAny(exitTask, timeoutTask) == timeoutTask)
                    {
                        process.Kill();
                    }
                }
                else
                {
                    process.Kill();
                }

                await WaitForExitAsync(process, 1000);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException)
            {
                // Process might have exited already
            }
        }

        // Helper method to wait for process exit asynchronously
        private Task<bool> WaitForExitAsync(Process process, int timeout = -1)
        {
            if (process.HasExited) return Task.FromResult(true);

            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(true);

            if (timeout > 0)
            {
                return Task.WhenAny(tcs.Task, Task.Delay(timeout))
                          .ContinueWith(t => t.Result == tcs.Task);
            }

            return tcs.Task;
        }
        public async Task<IEnumerable<string>?> GetCurrentPageOffersAsync()
        {
            await Task.Delay(2000); // Wait for page to load
            var xpathToFind = "//ul[contains(@class, 'semantic-search-results-list')]";
            var by = By.XPath(xpathToFind);
            var jobContanier = _driver.FindElements(by).FirstOrDefault();
            if (jobContanier == null)
            {
                return null;
            }
            var xpathToFindListItem = ".//li[contains(@class, 'semantic-search-results-list__list-item')]";
            var jobNodes = jobContanier.FindElements(By.XPath(xpathToFindListItem));
            if (jobNodes == null)
            {
                return null;
            }
            var offers = new List<string>();
            foreach (var jobNode in jobNodes)
            {
                try
                {
                    var xpathCard= ".//div[contains(@class, 'job-card-job-posting-card-wrapper')]";
                    var byCard = By.XPath(xpathCard);
                    var cards = jobNode.FindElements(byCard);
                    if (cards == null || cards.Count() == 0)
                    {
                        xpathCard = ".//div[contains(@class, 'semantic-search-results-list__list-item')]";
                        byCard = By.XPath(xpathCard);
                        cards = jobNode.FindElements(byCard);
                    }
                    if (cards == null || cards.Count() == 0)
                    {
                        offers.Add($"the card didn't find it {jobNode.GetAttribute("id")}");
                        continue;
                    }
                    var card = cards.FirstOrDefault();
                    var cssSelectorCardLink = "a.job-card-job-posting-card-wrapper__card-link";
                    var byCardLink = By.CssSelector(cssSelectorCardLink);
                    var jobAnchors = card.FindElements(byCardLink);
                    if (jobAnchors == null || jobAnchors.Count() == 0)
                    {
                        offers.Add("the anchor didn't find it");
                        continue;
                    }
                    var jobAnchor = jobAnchors.FirstOrDefault();
                    var jobUrl = jobAnchor.GetAttribute("href");
                    if (jobUrl == null)
                    {
                        offers.Add("the url didn't find it");
                        continue;
                    }
                    offers.Add(jobUrl);
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
