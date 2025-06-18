using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class JobOfferDetailProcessor : IJobOfferDetailProcessor
    {
        private readonly ILogger<JobOfferDetailProcessor> _logger;
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly List<JobOfferDetail> _offersDetail;
        private readonly ICaptureService _capture;
        private readonly string _executionFolder;

        public JobOfferDetailProcessor(IWebDriverFactory driverFactory,
            ILogger<JobOfferDetailProcessor> logger,
            ICaptureService capture,
            string executionFolder)
        {
            _offersDetail = new List<JobOfferDetail>();
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _capture = capture;
            _executionFolder = executionFolder;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers)
        {
            _logger.LogInformation("📝 Processing detailed job offer data...");

            foreach (var offer in offers)
            {
                try
                {
                    _logger.LogDebug($"🌐 Navigating to job offer URL: {offer}");
                    _driver.Navigate().GoToUrl(offer);
                    _wait.Until(driver =>
                    {
                        var xPathJobs = "//div[contains(@class, 'jobs-box--with-cta-large')]";
                        var el = driver.FindElements(By.XPath(xPathJobs)).FirstOrDefault();
                        return el != null && el.Displayed;
                    });
                    if (IsSecurityChek())
                    {
                        await TryStartPuzzle();
                    }

                    await _capture.CaptureArtifacts(_executionFolder, "Detailed Job offer");

                    var offersDetail = await ExtractDescriptionLinkedIn();
                    if (offersDetail != null)
                    {
                        _offersDetail.Add(offersDetail);
                        _logger.LogInformation("✅ Detailed job offer processed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Failed to process detailed job offer at URL: {offer}");
                    await _capture.CaptureArtifacts(_executionFolder, "Error in Detailed Job Offer");
                    // Continue with next offer instead of stopping
                }
            }
            return _offersDetail;
        }
        public async Task<JobOfferDetail> ExtractDescriptionLinkedIn()
        {
            _logger.LogDebug("🔍 Extracting job details from current page...");

            var details = _driver.FindElements(By.XPath("//div[contains(@class, 'jobs-box--with-cta-large')]"));
            if (!details.Any())
            {
                var message = $"❌ Job details container not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            var detail = details.FirstOrDefault(x => x != null);
            _logger.LogDebug("✅ Job details container found.");

            var seeMoreButtons = detail.FindElements(By.XPath("//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]"));
            if (!seeMoreButtons.Any())
            {
                var message = $"❌ 'See more' button not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            _logger.LogDebug("✅ 'See more' button found.");

            await _capture.CaptureArtifacts(_executionFolder, "ExtractDescriptionLinkedIn");

            var jobOffer = new JobOfferDetail();
            return jobOffer;
        }

        private bool IsSecurityChek()
        {
            var title = _driver.Title.Contains("Security Verification");
            var captcha = _driver.FindElements(By.Id("captcha-internal")).Any();
            var text = _driver.FindElements(By.XPath("//h1[contains(text(), 'Let’s do a quick security check')]")).Any();
            return title || captcha || text;
        }

        private async Task TryStartPuzzle()
        {
            try
            {
                _logger.LogDebug("🔎 Searching for 'Start Puzzle' button...");
                await _capture.CaptureArtifacts(_executionFolder, "Error in Detailed Job Offer");
                var startPuzzleButton = _wait.Until(driver =>
                {
                    var xpathText = "//button[contains(text(), 'Start Puzzle')]";
                    var button = driver.FindElements(By.XPath(xpathText))
                                                   .FirstOrDefault();
                    return (button != null && button.Displayed && button.Enabled) ? button : null;
                });

                await _capture.CaptureArtifacts(_executionFolder, "Error in Detailed Job Offer");

                if (startPuzzleButton == null)
                {
                    _logger.LogWarning("⚠️ 'Start Puzzle' button not found on security check page.");
                }

                if (!startPuzzleButton.Displayed || !startPuzzleButton.Enabled)
                {
                    _logger.LogWarning("⚠️ 'Start Puzzle' button is not interactable.");
                }

                _logger.LogInformation("🧩 Clicking 'Start Puzzle' button...");
                startPuzzleButton.Click();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to click 'Start Puzzle' button.");
            }
        }
    }
}
