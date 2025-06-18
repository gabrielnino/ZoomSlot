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
        private readonly string _executionFolder;
        private readonly string _folderName;
        private readonly string _timestamp;
        private readonly ICaptureService _capture;
        private readonly List<string> _offers;
        private readonly List<JobOfferDetail> _offersDetail;
        
        public LinkedInService(
            IWebDriverFactory driverFactory,
            AppConfig config,
            ILogger<LinkedInService> logger,
            CommandArgs commandArgs,
            ILoginService loginService,
            ICaptureService capture)
        {
            _driver = driverFactory.Create();
            _config = config;
            _logger = logger;

            _timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _folderName = $"Execution_{_timestamp}";
            _executionFolder = Path.Combine(Directory.GetCurrentDirectory(), _folderName);
            EnsureDirectoryExists(_executionFolder);
            _logger.LogInformation($"📁 Created execution folder at: {_executionFolder}");
            _loginService = loginService;
            _capture = capture;
            _offers = [];
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _offersDetail = [];
        }

        private void EnsureDirectoryExists(string path)
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
                _logger.LogInformation("🚀 Starting LinkedIn job search process");
                await _loginService.LoginAsync(_folderName, _timestamp);
                await PerformSearchAsync();
                await ProcessAllPagesAsync();
                ProcessDetailedJobOffers();
                _logger.LogInformation("✅ Job search completed successfully");
            }
            catch (Exception ex)
            {
                await _capture.CaptureArtifacts(_executionFolder, ErrorMessage);
                _logger.LogError(ex, $"❌ Critical error during job search. Debug info saved to:\nHTML: {_timestamp}.html\nScreenshot: {_timestamp}.png");
                throw new ApplicationException("Job search failed. See inner exception for details.", ex);
            }
            finally
            {
                _logger.LogInformation("🧹 Cleaning up resources after job search process");
                Dispose();
            }
        }

        

        private async Task PerformSearchAsync()
        {
            _logger.LogInformation("🔍 Navigating to LinkedIn Jobs page...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/jobs");
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(_executionFolder, "JobsPageLoaded");
            var search = By.XPath("//input[contains(@class, 'jobs-search-box__text-input')]");
            var searchInput = _driver.FindElements(search).FirstOrDefault();
            if (searchInput == null)
            {
                var message = $"Job search input not found on page. Current URL: {_driver.Url}";
                throw new InvalidOperationException(message);
            }
            _logger.LogInformation($"🔎 Searching for: '{_config.JobSearch.SearchText}'");
            searchInput.SendKeys(_config.JobSearch.SearchText + Keys.Enter);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(_executionFolder, "SearchExecuted");
            _logger.LogInformation($"✅ Search completed for: '{_config.JobSearch.SearchText}'");
        }

        private async Task ProcessAllPagesAsync()
        {
            int pageCount = 0;
            _logger.LogInformation($"📄 Processing up to {_config.JobSearch.MaxPages} pages of results");

            do
            {
                ScrollMove();
                await Task.Delay(3000);
                pageCount++;
                _logger.LogInformation($"📖 Processing page {pageCount}...");

                var pageOffers = await GetCurrentPageOffersAsync();
                if(pageOffers == null)
                {
                    continue;
                }
                _offers.AddRange(pageOffers);
                _logger.LogInformation($"✔️ Page {pageCount} processed. Found {pageOffers?.Count() ?? 0} listings");
                if (pageCount >= _config.JobSearch.MaxPages)
                {
                    _logger.LogInformation($"ℹ️ Reached maximum page limit of {_config.JobSearch.MaxPages}");
                    break;
                }

            } while (await NavigateToNextPageAsync());
        }

        private void ScrollMove()
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
                        var url = ExtractJobIdUrl("https://www.linkedin.com", jobUrl);
                        if(string.IsNullOrWhiteSpace(url))
                        {
                            continue;
                        }
                        offers.Add(url);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"⚠️ Failed to process job listing {jobNode.GetAttribute("id")}");
                }
            }

            return offers;
        }

        private string? ExtractJobIdUrl(string urlLinkedin, string url)
        {
            var uri = new Uri(url);
            string[] segments = uri.Segments;
            if (segments.Length >= 4 && segments[2].Equals("view/", StringComparison.OrdinalIgnoreCase))
            {
                string jobId = segments[3].TrimEnd('/');
                return $"{urlLinkedin}/jobs/view/{jobId}/";
            }

            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (queryParams["currentJobId"] != null)
            {
                string jobId = queryParams["currentJobId"];
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
                var nextButton = _driver.FindElements(By.XPath("//div[contains(@class, 'semantic-search-results-list__pagination')]"))
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

        private async Task ProcessDetailedJobOffers()
        {
            foreach (var offer in _offers)
            {
                _driver.Navigate().GoToUrl(offer);

                await _capture.CaptureArtifacts(_executionFolder, "Detailed Job offer");
                var offersDetail = await ExtractDescriptionLinkedIn();
                if (offersDetail == null)
                {
                    continue;
                }
                _offersDetail.Add(offersDetail);
            }
        }

        public async Task<JobOfferDetail> ExtractDescriptionLinkedIn()
        {
            var details = _driver.FindElements(By.XPath("//div[contains(@class, 'jobs-box--with-cta-large')]"));
            if (!details.Any())
            {
                var message = $"Job details container not found on the page. Current URL: {_driver.Url}";
                throw new InvalidOperationException(message);
            }
            var detail = details.FirstOrDefault();

            var seeMoreButtons = detail.FindElements(By.XPath("//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]"));
            if (!seeMoreButtons.Any())
            {
                var message = $"'See more' button not found in job description. Current URL: {_driver.Url}";
                throw new InvalidOperationException(message);
            }

            await _capture.CaptureArtifacts(_executionFolder, "Detailed Job offer");
            var jobOffer = new JobOfferDetail();
            /*
            var seeMoreButton = seeMoreButtons.First();
            if (seeMoreButton.Displayed)
            {
                seeMoreButton.Click();
            }

            //jobs-details
            //var details = _driver.FindElement(By.XPath("//div[contains(@class, 'jobs-semantic-search-job-details-wrapper')]"));
            var header = details.FindElement(By.CssSelector("div.t-14.artdeco-card"));
            var job_title_element = header.FindElement(By.CssSelector("h1.t-24.t-bold.inline"));
            var jobOffer = new JobOfferDetail
            {
                //title
                JobOfferTitle = job_title_element.Text
            };
            var company_name_element = header.FindElement(By.CssSelector(".job-details-jobs-unified-top-card__company-name a"));
            //company name
            jobOffer.CompanyName = company_name_element.Text;
            //
            var hiring_team_section = details.FindElement(By.CssSelector("div.job-details-module"));
            var name_elements = hiring_team_section.FindElements(By.CssSelector(".jobs-poster__name strong"));
            if (name_elements.Any())
            {
                var name_element = name_elements.First();
                //hiring_team_section
                jobOffer.ContactHiringSection = name_element.Text;
            }
            var applicants = _wait.Until(driver => driver.FindElement(By.XPath(
                "//div[contains(@class, 'job-details-jobs-unified-top-card__primary-description-container')]"
            )));
            if (applicants != null)
            {
                jobOffer.Applicants = applicants.Text;
            }

            //var seeMoreButtons = details.FindElements(By.XPath("//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]"));
            //if (seeMoreButtons.Any())
            //{
            //    var seeMoreButton = seeMoreButtons.First();
            //    if (seeMoreButton.Displayed)
            //    {
            //        seeMoreButton.Click();
            //    }
            //}

            var description_element = details.FindElement(By.CssSelector("article.jobs-description__container"));
            //description
            jobOffer.Description = description_element.Text;
            //var salaryContainer = driver.FindElement(By.Id("SALARY"));
            var jobDetailsContainers = details.FindElements(By.CssSelector(".artdeco-card.job-details-module"));
            if (jobDetailsContainers.Any())
            {
                var salaryElements = _driver.FindElements(By.XPath(".//p[contains(., 'CA$')]"));
                if (salaryElements.Any())
                {
                    var salaryElement = salaryElements.First();
                    jobOffer.SalaryOrBudgetOffered = salaryElement.Text.Trim();
                }

            }
            */
            return jobOffer;
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