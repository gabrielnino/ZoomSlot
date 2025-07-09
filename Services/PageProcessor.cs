using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class PageProcessor : IPageProcessor
    {
        private readonly IWebDriver _driver;
        private readonly AppConfig _config;
        private readonly ILogger<PageProcessor> _logger;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Page";
        private readonly ISecurityCheck _securityCheck;
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly ICaptureSnapshot _capture;
        private readonly IDirectoryCheck _directoryCheck;
        private readonly IPageTrackingService _trackingService;
        private readonly IJobSearch _jobSearch;
        private readonly WebDriverWait _wait;

        public PageProcessor(IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<PageProcessor> logger,
            ExecutionOptions executionOptions,
            ICaptureSnapshot capture,
            ISecurityCheck securityCheck,
            IDirectoryCheck directoryCheck,
            IPageTrackingService trackingService,
            IJobSearch jobSearch)
        {
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;
            _executionOptions = executionOptions;
            _capture = capture;
            _securityCheck = securityCheck;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _trackingService = trackingService;
            _jobSearch = jobSearch;
        }

        public async Task<List<string>> ProcessAllPagesAsync()
        {
            // Create unique search ID based on search text and timestamp
            var searchId = $"{StringHelpers.NormalizeJobKeywords(_config.JobSearch.SearchText)}_{_executionOptions.TimeStamp}";

            // Load tracking state BEFORE doing any work
            var trackingState = await _trackingService.LoadPageStateAsync(searchId);
            _logger.LogInformation($"ℹ️ ID:{_executionOptions.TimeStamp} Loaded tracking state: Page {trackingState.LastProcessedPage}, Offers {trackingState.CollectedOffers.Count}");

            // Check if we have a complete previous run
            if (trackingState.IsComplete && trackingState.CollectedOffers.Any())
            {
                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Using cached results from previous complete search");
                return trackingState.CollectedOffers;
            }

            // Initialize variables based on tracking state
            int startPage = trackingState.LastProcessedPage + 1;
            var offers = new List<string>(trackingState.CollectedOffers);

            // Only perform new search if we're not resuming
            //if (startPage == 1)
            //{
            //    _logger.LogInformation($"🔍 ID:{_executionOptions.TimeStamp} Starting new search");
            //    await _jobSearch.PerformSearchAsync();
            //}
            //else
            //{
            //    _logger.LogInformation($"↩️ ID:{_executionOptions.TimeStamp} Resuming from page {startPage}");
            //    await NavigateToSpecificPage(startPage - 1); // Navigate to last successfully processed page
            //}

            // Process pages starting from where we left off
            for (int currentPage = startPage; currentPage < _config.JobSearch.MaxPages; currentPage++)
            {
                _logger.LogInformation($"📖 ID:{_executionOptions.TimeStamp} Processing page {currentPage}...");

                await _capture.CaptureArtifactsAsync(FolderPath, $"Page_{currentPage}");
                ScrollMove();
                await Task.Delay(3000);

                var pageOffers = await GetCurrentPageOffersAsync();
                if (pageOffers == null || !pageOffers.Any())
                {
                    _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} No offers found on page {currentPage}");
                    break;
                }

                offers.AddRange(pageOffers);

                // Update tracking state after each successful page
                trackingState.LastProcessedPage = currentPage;
                trackingState.CollectedOffers = offers;
                await _trackingService.SavePageStateAsync(searchId, trackingState);

                _logger.LogInformation($"✔️ ID:{_executionOptions.TimeStamp} Page {currentPage} processed. Found {pageOffers.Count()} offers");

                // Check if we should continue to next page
                if (currentPage >= _config.JobSearch.MaxPages || !await NavigateToNextPageAsync())
                {
                    trackingState.IsComplete = true;
                    await _trackingService.SavePageStateAsync(searchId, trackingState);
                    break;
                }
            }

            _logger.LogInformation($"🏁 ID:{_executionOptions.TimeStamp} Completed processing. Total offers: {offers.Count}");
            return offers;
        }

        private async Task NavigateToSpecificPage(int targetPage)
        {
            _logger.LogInformation($"↩️ ID:{_executionOptions.TimeStamp} Attempting to navigate to page {targetPage + 1}");

            try
            {
                // First try direct URL navigation if supported
                var currentUrl = new Uri(_driver.Url);
                var queryParams = System.Web.HttpUtility.ParseQueryString(currentUrl.Query);
                queryParams["pageNum"] = (targetPage + 1).ToString();
                var newUrl = $"{currentUrl.GetLeftPart(UriPartial.Path)}?{queryParams}";

                _driver.Navigate().GoToUrl(newUrl);
                await Task.Delay(5000);

                // Verify we're on the correct page
                string xpathLi = "//li[contains(@class,'active')]/span";
                var activePageIndicator = _wait.Until(driver => driver.FindElements(By.XPath(xpathLi))).FirstOrDefault()?.Text;

                if (activePageIndicator != null && activePageIndicator.Trim() == (targetPage + 1).ToString())
                {
                    _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully navigated to page {targetPage + 1}");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"⚠️ ID:{_executionOptions.TimeStamp} Direct page navigation failed");
            }

            // Fallback: paginate from beginning
            _logger.LogInformation($"🔄 ID:{_executionOptions.TimeStamp} Falling back to sequential pagination");
            await _jobSearch.PerformSearchAsync();

            for (int i = 1; i <= targetPage; i++)
            {
                if (!await NavigateToNextPageAsync())
                {
                    throw new InvalidOperationException($"Failed to paginate to page {targetPage + 1}");
                }
            }
        }

        private string? ExtractJobIdUrl(string urlLinkedin, string url)
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            if (segments.Length >= 4 && segments[2].Equals("view/", StringComparison.OrdinalIgnoreCase))
            {
                var jobId = segments[3].TrimEnd('/');
                return $"{urlLinkedin}/jobs/view/{jobId}/";
            }

            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (queryParams["currentJobId"] != null)
            {
                var jobId = queryParams["currentJobId"];
                return $"{urlLinkedin}/jobs/view/{jobId}/";
            }

            return null;
        }

        private string? ExtractJobUrl(IWebElement jobNode)
        {
            var card = jobNode.FindElements(By.XPath(".//div[contains(@class, 'job-card-job-posting-card-wrapper')]"))
                              .FirstOrDefault()
                    ?? jobNode.FindElements(By.XPath(".//div[contains(@class, 'semantic-search-results-list__list-item')]"))
                              .FirstOrDefault();

            if (card == null)
            {
                throw new Exception($"❌ ID:{_executionOptions.TimeStamp} Job card element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobAnchor = card.FindElements(By.CssSelector("a.job-card-job-posting-card-wrapper__card-link"))
                                .FirstOrDefault();

            if (jobAnchor == null)
            {
                throw new Exception($"❌ ID:{_executionOptions.TimeStamp} Job link element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobUrl = jobAnchor.GetAttribute("href");
            if (string.IsNullOrEmpty(jobUrl))
            {
                throw new Exception($"❌ ID:{_executionOptions.TimeStamp} Empty URL in listing {jobNode.GetAttribute("id")}");
            }

            return jobUrl;
        }

        private void ScrollMove()
        {
            var scrollable = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]"))
                                    .FirstOrDefault();

            if (scrollable == null)
            {
                _logger.LogWarning("⚠️ ID:{_executionOptions.TimeStamp} Scrollable results container not found; skipping scroll operation.");
                return;
            }

            var jsExecutor = (IJavaScriptExecutor)_driver;
            long scrollHeight = (long)jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollable);
            long currentPosition = 0;

            _logger.LogDebug($"🖱️ ID:{_executionOptions.TimeStamp} Scrolling through job results container (total height: {scrollHeight}px)...");

            while (currentPosition < scrollHeight)
            {
                currentPosition += 10;
                jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[1];", scrollable, currentPosition);
                Thread.Sleep(20);
            }

            _logger.LogDebug($"🖱️ ID:{_executionOptions.TimeStamp} Scrolling completed.");
        }

        private async Task<bool> NavigateToNextPageAsync()
        {
            try
            {
                //  "//div[contains(@class, 'semantic-search-results-list__pagination')]"

                string xpath = "//button[@aria-label='View next page' and .//span[text()='Next']]";
                var nextButton = _driver.FindElements(By.XPath(xpath))
                                        .FirstOrDefault(b => b.Enabled);

                if (nextButton == null)
                {
                    _logger.LogInformation($"⏹️ ID:{_executionOptions.TimeStamp} No additional results pages detected; pagination completed.");
                    return false;
                }

                _logger.LogDebug($"⏭️ ID:{_executionOptions.TimeStamp} Clicking to navigate to next page...");
                nextButton.Click();
                await Task.Delay(3000);

                if (_securityCheck.IsSecurityCheck())
                {
                    await _securityCheck.HandleSecurityPage();
                    throw new InvalidOperationException(
                        $"❌ ID:{_executionOptions.TimeStamp} LinkedIn requires manual security verification. Please complete verification in the browser before proceeding.");
                }

                // Optional: If you want to verify page load consistency here
                var jobContainer = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]")).FirstOrDefault();
                if (jobContainer == null)
                {
                    await _securityCheck.HandleUnexpectedPage();
                    throw new InvalidOperationException(
                        $"❌ ID:{_executionOptions.TimeStamp} Failed to load next page of job listings. Current URL: {_driver.Url}");
                }

                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully navigated to the next page of results.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"⚠️ ID:{_executionOptions.TimeStamp} Exception encountered while navigating to the next page.");
                return false;
            }
        }

        private async Task<IEnumerable<string>?> GetCurrentPageOffersAsync()
        {
            await Task.Delay(2000);

            var jobContainer = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]"))
                                      .FirstOrDefault();

            if (jobContainer == null)
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} No job listings container found on the current page.");
                return null;
            }

            var jobNodes = jobContainer.FindElements(By.XPath(".//li[contains(@class, 'semantic-search-results-list__list-item')]"));
            if (jobNodes == null || !jobNodes.Any())
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} No job listings detected on the current page.");
                return null;
            }

            _logger.LogDebug($"🔍 ID:{_executionOptions.TimeStamp} Detected {jobNodes.Count} job listings on the current page.");

            var offers = new List<string>();
            foreach (var jobNode in jobNodes)
            {
                try
                {
                    var jobUrl = ExtractJobUrl(jobNode);
                    if (!string.IsNullOrEmpty(jobUrl))
                    {
                        var url = ExtractJobIdUrl("https://www.linkedin.com", jobUrl);
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            offers.Add(url);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ ID:{_executionOptions.TimeStamp} Failed to extract job URL for listing with ID: {jobNode.GetAttribute("id")}");
                }
            }

            return offers;
        }
    }
}
