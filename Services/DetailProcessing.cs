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
        private readonly List<JobOfferDetail> _offersDetail;
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

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
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

                    var offersDetail = await ExtractDetail(searchText);
                    if (offersDetail != null)
                    {
                        offersDetail.SearchText = searchText;
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
        public async Task<JobOfferDetail> ExtractDetail(string searchText)
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
            if (seeMoreButtons.Any())
            {
                var seeMoreButton = seeMoreButtons.FirstOrDefault(x => x != null);
                seeMoreButton.Click();
            }
            _logger.LogDebug($"✅ ID:{_executionOptions.TimeStamp} 'See more' button found.");
            await _capture.CaptureArtifacts(FolderPath, "ExtractDescriptionLinkedIn");
            var headers = _driver.FindElements(By.XPath("//div[contains(@class, 't-14') and contains(@class, 'artdeco-card')]"));
            if (!headers.Any())
            {
                var message = $"❌ ID:{_executionOptions.TimeStamp} 'Header not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }
            var header = headers.FirstOrDefault(x => x != null);
            var job_title_elements = header.FindElements(By.CssSelector("h1.t-24.t-bold.inline"));
            var jobOfferTitle = string.Empty;
            if (job_title_elements.Any())
            {
                var job_title_element = job_title_elements.FirstOrDefault(x => x != null);
                if (!string.IsNullOrWhiteSpace(job_title_element.Text))
                {
                    jobOfferTitle = job_title_element.Text;
                }
            }
            var company_name_elements = header.FindElements(By.CssSelector(".job-details-jobs-unified-top-card__company-name a"));
            var companyName = string.Empty;
            if (company_name_elements.Any())
            {
                var company_name_element = company_name_elements.FirstOrDefault(x => x != null);
                if (!string.IsNullOrWhiteSpace(company_name_element.Text))
                {
                    companyName = company_name_element.Text;
                }
            }
            var hiring_team_sections = detail.FindElements(By.CssSelector("div.job-details-module"));
            var contactHiringSection = string.Empty;
            if (hiring_team_sections.Any())
            {
                var hiring_team_section = hiring_team_sections.FirstOrDefault(x => x != null);
                var name_elements = hiring_team_section.FindElements(By.CssSelector(".jobs-poster__name strong"));
                if (name_elements.Any())
                {
                    var name_element = name_elements.FirstOrDefault(x => x != null);
                    if (!string.IsNullOrWhiteSpace(name_element.Text))
                    {
                        contactHiringSection = name_element.Text;
                    }
                }
            }

            var aplicantsXPath = "//div[contains(@class, 'job-details-jobs-unified-top-card__primary-description-container')]";
            var applicants = _wait.Until(driver => _driver.FindElements(By.XPath(aplicantsXPath)));
            var applicantsText = string.Empty;
            if (applicants.Any())
            {
                var applicant = applicants.FirstOrDefault(x => x != null);
                applicantsText = applicant.Text;
            }
            var description_elements = detail.FindElements(By.CssSelector("article.jobs-description__container"));
            var descriptionText = string.Empty;
            if (description_elements.Any())
            {
                var description_element = description_elements.FirstOrDefault(x => x != null);
                if (!string.IsNullOrWhiteSpace(description_element.Text))
                {
                    descriptionText = description_element.Text;
                }
            }
            var jobDetailsContainers = detail.FindElements(By.CssSelector(".artdeco-card.job-details-module"));
            var salaryOrBudgetOffered = string.Empty;
            if (jobDetailsContainers.Any())
            {
                var salaryElements = _driver.FindElements(By.XPath(".//p[contains(., 'CA$')]"));
                if (salaryElements.Any())
                {
                    var salaryElement = salaryElements.First();
                    salaryOrBudgetOffered = salaryElement.Text.Trim();
                }

            }
            var jobOffer = new JobOfferDetail
            {
                JobOfferTitle = jobOfferTitle,
                CompanyName = companyName,
                ContactHiringSection = contactHiringSection,
                Applicants = applicantsText,
                Description = descriptionText,
                SalaryOrBudgetOffered = salaryOrBudgetOffered,
                Link = _driver.Url,
                SearchText = searchText,

            };
            return jobOffer;
        }
    }
}
