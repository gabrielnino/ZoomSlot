using System.Diagnostics;
using System.Reactive;
using System.Text.RegularExpressions;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class LinkedInService : ILinkedInService, IDisposable
    {
        private const string ErrorMessage = "Job search operation failed";
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly AppConfig _config;
        private readonly ILogger<LinkedInService> _logger;
        private bool _disposed = false;
        private readonly ILoginService _loginService;
        private readonly ExecutionOptions _executionOptions;
        private readonly ICaptureService _capture;
        private readonly List<string> _offers;
        private readonly IJobOfferDetailProcessor _jobOfferDetail;



        public LinkedInService(
            IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<LinkedInService> logger,
            CommandArgs commandArgs,
            ILoginService loginService,
            ICaptureService capture,
            ExecutionOptions executionOptions,
            IJobOfferDetailProcessor jobOfferDetail)
        {
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;
            _executionOptions = executionOptions;
            EnsureDirectoryExists(_executionOptions.ExecutionFolder);
            _logger.LogInformation($"📁 Created execution folder at: {_executionOptions.ExecutionFolder}");

            _loginService = loginService;
            _capture = capture;
            _offers = [];
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _jobOfferDetail = jobOfferDetail;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public async Task SearchJobsAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Starting LinkedIn job search process...");
                await _loginService.LoginAsync();
                await PerformSearchAsync();
                await ProcessAllPagesAsync();
                await _jobOfferDetail.ProcessOffersAsync(_offers);
                _logger.LogInformation("✅ LinkedIn job search process completed successfully.");
            }
            catch (Exception ex)
            {
                var timestamp = await _capture.CaptureArtifacts(_executionOptions.ExecutionFolder, "An unexpected error");
                _logger.LogError(ex, $"❌ An unexpected error occurred during the LinkedIn job search process. Debug artifacts saved at:\nHTML: {timestamp}.html\nScreenshot: {timestamp}.png");
                throw new ApplicationException("Job search failed. See inner exception for details.", ex);
            }
            finally
            {
                _logger.LogInformation("🧹 Cleaning up resources after job search process...");
                Dispose();
            }
        }



        private async Task PerformSearchAsync()
        {
            _logger.LogInformation("🔍 Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(_executionOptions.ExecutionFolder, "JobsPageLoaded");

            var searchInput = _driver.FindElements(By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]"))
                                     .FirstOrDefault();
            if (searchInput == null)
            {
                throw new InvalidOperationException($"❌ Job search input field not found. Current URL: {_driver.Url}");
            }

            _logger.LogInformation($"🔎 Executing job search with keyword: '{_config.JobSearch.SearchText}'...");
            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(_executionOptions.ExecutionFolder, "SearchExecuted");
            _logger.LogInformation($"✅ Search executed for: '{_config.JobSearch.SearchText}'.");
        }

        private async Task ProcessAllPagesAsync()
        {
            int pageCount = 0;
            _logger.LogInformation($"📄 Beginning processing of up to {_config.JobSearch.MaxPages} result pages...");

            do
            {
                ScrollMove();
                await Task.Delay(3000);
                pageCount++;
                _logger.LogInformation($"📖 Processing results page {pageCount}...");

                var pageOffers = await GetCurrentPageOffersAsync();
                if (pageOffers == null) continue;

                _offers.AddRange(pageOffers);
                _logger.LogInformation($"✔️ Results page {pageCount} processed. Found {pageOffers.Count()} listings.");

                if (pageCount >= _config.JobSearch.MaxPages)
                {
                    _logger.LogInformation($"ℹ️ Reached maximum configured page limit of {_config.JobSearch.MaxPages}.");
                    break;
                }

            } while (await NavigateToNextPageAsync());
        }

        private void ScrollMove()
        {
            var scrollable = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]"))
                                    .FirstOrDefault();

            if (scrollable == null)
            {
                _logger.LogWarning("⚠️ Scrollable results container not found; skipping scroll operation.");
                return;
            }

            var jsExecutor = (IJavaScriptExecutor)_driver;
            long scrollHeight = (long)jsExecutor.ExecuteScript("return arguments[0].scrollHeight", scrollable);
            long currentPosition = 0;

            _logger.LogDebug($"🖱️ Scrolling through job results container (total height: {scrollHeight}px)...");

            while (currentPosition < scrollHeight)
            {
                currentPosition += 10;
                jsExecutor.ExecuteScript("arguments[0].scrollTop = arguments[1];", scrollable, currentPosition);
                Thread.Sleep(50);
            }

            _logger.LogDebug("🖱️ Scrolling completed.");
        }

        public async Task<IEnumerable<string>?> GetCurrentPageOffersAsync()
        {
            await Task.Delay(2000);

            var jobContainer = _driver.FindElements(By.XPath("//ul[contains(@class, 'semantic-search-results-list')]"))
                                      .FirstOrDefault();

            if (jobContainer == null)
            {
                _logger.LogWarning("⚠️ No job listings container found on the current page.");
                return null;
            }

            var jobNodes = jobContainer.FindElements(By.XPath(".//li[contains(@class, 'semantic-search-results-list__list-item')]"));
            if (jobNodes == null || !jobNodes.Any())
            {
                _logger.LogWarning("⚠️ No job listings detected on the current page.");
                return null;
            }

            _logger.LogDebug($"🔍 Detected {jobNodes.Count} job listings on the current page.");

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
                    _logger.LogWarning(ex, $"⚠️ Failed to extract job URL for listing with ID: {jobNode.GetAttribute("id")}");
                }
            }

            return offers;
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
                throw new Exception($"❌ Job card element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobAnchor = card.FindElements(By.CssSelector("a.job-card-job-posting-card-wrapper__card-link"))
                                .FirstOrDefault();

            if (jobAnchor == null)
            {
                throw new Exception($"❌ Job link element not found in listing {jobNode.GetAttribute("id")}");
            }

            var jobUrl = jobAnchor.GetAttribute("href");
            if (string.IsNullOrEmpty(jobUrl))
            {
                throw new Exception($"❌ Empty URL in listing {jobNode.GetAttribute("id")}");
            }

            return jobUrl;
        }

        public async Task<bool> NavigateToNextPageAsync()
        {
            try
            {
                var nextButton = _driver.FindElements(By.XPath("//div[contains(@class, 'semantic-search-results-list__pagination')]"))
                                        .FirstOrDefault(b => b.Enabled);

                if (nextButton == null)
                {
                    _logger.LogInformation("⏹️ No additional results pages detected; ending pagination.");
                    return false;
                }

                _logger.LogDebug("⏭️ Clicking to navigate to next page...");
                nextButton.Click();
                await Task.Delay(3000);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to navigate to the next page of results.");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger.LogDebug("🧹 Disposing browser driver and cleaning resources...");
                _driver?.Quit();
                _driver?.Dispose();
                _logger.LogInformation("✅ Browser driver and resources disposed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception encountered while disposing browser resources.");
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
