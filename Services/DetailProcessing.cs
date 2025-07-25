﻿using System;
using System.Text.Json;
using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class DetailProcessing : IDetailProcessing
    {
        private WebDriverWait _wait;
        private readonly ILogger<DetailProcessing> _logger;
        private readonly IWebDriverFactory _driverFactory;  
        private IWebDriver _driver;
        private readonly List<JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private readonly ILoginService _loginService;
        private readonly IJobStorageService _jobStorageService;
        private readonly ISecurityCheck _securityCheck;
        private readonly IUtil _util;
        private const string FolderName = "Detail";
        
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers.json");
        private string OffersDetailFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers_detail.json");

        private List<string> _offersPending;

        public DetailProcessing(
            IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ILoginService loginService,
            IJobStorageService jobStorageService,
            ISecurityCheck securityCheck,
            IUtil util)
        {
            _logger = logger;
            _offersDetail = [];
            _capture = capture;
            _executionOptions = executionOptions;
            _offersPending = LoadPendingOffers(OffersFilePath);
            _offersDetail = LoadJobsDetailAsync(OffersDetailFilePath);
            _loginService = loginService;
            _jobStorageService = jobStorageService;
            _securityCheck = securityCheck;
            _util = util;
            _driverFactory = driverFactory;
        }

        public async Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText)
        {
            try
            {
                if (_offersPending != null && _offersPending.Any())
                {
                    offers = _offersPending;
                    _logger.LogInformation($"📂 ID:{_executionOptions.TimeStamp} Resuming processing from existing offers.json with {offers.Count()} pending offers.");
                }
                var offerList = offers.ToList();
                int totalOffers = offerList.Count;
                if (totalOffers == 0)
                {
                    _logger.LogWarning("ID:{TimeStamp} - No offers provided to process.", _executionOptions.TimeStamp);
                    return [];
                }
                var delays = new[] { 30, 50, 70, 100, 200 };
                var random = new Random();
                await Process(offerList, searchText);
                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Completed processing of all job offers. Total offers processed: {_offersDetail.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Unexpected error occurred while processing offer: {offers}");
                await _capture.CaptureArtifactsAsync(FolderPath, "GeneralError");
            }
            return _offersDetail;
        }

        private async Task Process(IEnumerable<string> offers, string searchText)
        {
            _logger.LogInformation($"🔐 ID:{_executionOptions.TimeStamp} Logging into LinkedIn...");
            _driver = _driverFactory.Create();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(120));
            await _loginService.LoginAsync();
            if (_securityCheck.IsSecurityCheck())
            {
                _logger.LogWarning($"🛡️ ID:{_executionOptions.TimeStamp} Security check detected. Attempting to solve puzzle...");
                await _securityCheck.TryStartPuzzle();
            }
            _offersPending = offers.ToList();
            foreach (var offer in offers)
            {
                try
                {
                    _logger.LogInformation($"🌐 ID:{_executionOptions.TimeStamp} Navigating to job offer URL: {offer}");
                    try
                    {
                        _driver.Navigate().GoToUrl(offer);
                    }
                    catch
                    {
                        var delays = new[] { 3, 5, 7, 10, 2 };
                        var random = new Random();
                        var minutes = delays[random.Next(delays.Length)];
                        var delay = TimeSpan.FromMinutes(minutes);

                        _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} Initial navigation failed. Retrying after {minutes} minutes... (Press any key to skip waiting)");

                        // Create a cancellation token source
                        var cts = new CancellationTokenSource();

                        // Start a task to monitor keyboard input
                        var keyboardTask = Task.Run(() =>
                        {
                            if (Console.KeyAvailable)
                            {
                                Console.ReadKey(true); // Clear the buffer
                                return;
                            }

                            Console.ReadKey(true); // Wait for any key press
                            cts.Cancel();
                        });

                        try
                        {
                            // Wait for either the delay or a key press
                            await Task.Delay(delay, cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogInformation($"⏩ ID:{_executionOptions.TimeStamp} Wait interrupted by user. Continuing immediately...");
                        }

                        // Clean up the keyboard task if it's still running
                        if (!keyboardTask.IsCompleted)
                        {
                            // Give it a short time to complete if a key was just pressed
                            await Task.WhenAny(keyboardTask, Task.Delay(100));
                        }
                        _driver.Navigate().GoToUrl(offer);
                    }
                    _logger.LogInformation($"🔍 ID:{_executionOptions.TimeStamp} Waiting for job details section to load...");
                    _wait.Until(driver =>
                    {
                        var xPathJobs = "//div[contains(@class, 'jobs-box--with-cta-large')]";
                        var el = driver.FindElements(By.XPath(xPathJobs)).FirstOrDefault();
                        return el != null && el.Displayed;
                    });
                    await _capture.CaptureArtifactsAsync(FolderPath, "Detailed Job offer");
                    if (_securityCheck.IsSecurityCheck())
                    {
                        _logger.LogWarning($"🛡️ ID:{_executionOptions.TimeStamp} Security check triggered again. Retrying puzzle...");
                        await _securityCheck.TryStartPuzzle();
                    }
                    var offersDetail = await ExtractDetailAsync(searchText);
                    if (offersDetail == null)
                    {
                        _logger.LogWarning($"❌ ID:{_executionOptions.TimeStamp} Failed to extract details for offer: {offer}");
                        continue;
                    }
                    _offersDetail.Add(offersDetail);
                    _offersPending.Remove(offer);
                    await _jobStorageService.SaveOffersAsync(OffersFilePath, _offersPending);
                    await _jobStorageService.SaveJobOfferDetailAsync(OffersDetailFilePath, _offersDetail);
                    _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully processed and saved job offer: {offer}");
                }
                catch (WebDriverException ex)
                {
                    _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} WebDriver error occurred while processing offer: {offer}");
                    await _capture.CaptureArtifactsAsync(FolderPath, "WebDriverError");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Unexpected error occurred while processing offer: {offer}");
                    await _capture.CaptureArtifactsAsync(FolderPath, "GeneralError");
                }
            }
            _logger.LogInformation($"📦 ID:{_executionOptions.TimeStamp} Finished processing. Total offers processed: {_offersDetail.Count}, remaining: {_offersPending.Count}");
        }

        public List<string> LoadPendingOffers(string offersFilePath)
        {
            try
            {
                if (File.Exists(offersFilePath))
                {
                    var json = File.ReadAllText(offersFilePath);
                    var urls = JsonSerializer.Deserialize<List<string>>(json);
                    _logger.LogInformation("Loaded {Count} pending offers from {Path}", urls?.Count ?? 0, offersFilePath);
                    return urls ?? [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending offers from file");
            }

            return [];
        }

        public List<JobOfferDetail> LoadJobsDetailAsync(string offersFilePath)
        {
            try
            {
                if (File.Exists(offersFilePath))
                {
                    var json = File.ReadAllText(offersFilePath);
                    var jobOfferDetail = JsonSerializer.Deserialize<List<JobOfferDetail>>(json);
                    _logger.LogInformation("Loaded {Count} pending offers from {Path}", jobOfferDetail?.Count ?? 0, offersFilePath);
                    return jobOfferDetail ?? [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending offers from file");
            }

            return [];
        }

        private async Task<JobOfferDetail> ExtractDetailAsync(string searchText)
        {
            _logger.LogDebug($"🔍 ID:{_executionOptions.TimeStamp} Extracting job details from current page...");
            await _capture.CaptureArtifactsAsync(FolderPath, "ExtractDescription_Start");
            var detail = ExtractDetail();
            await _capture.CaptureArtifactsAsync(FolderPath, "ExtractDescription_AfterSeeMore");
            var header = ExtractHeader();
            var jobOfferTitle = ExtractTitle(header);
            var companyName = ExtractCompany(header);
            var contactHiringSection = ExtractContactHiring(detail);
            var applicants = ExtractApplicants();
            var descriptionText = ExtractDescription(detail);
            var salaryOrBudgetOffered = ExtractSalary(detail);
            var url = _driver.Url;
            var id = _util.ExtractJobId(url);
            if (id == null)
            {
                throw new ArgumentException($"Invalid url: {url} does not have a valid ID");
            }
            return new JobOfferDetail
            {
                ID = id,
                JobOfferTitle = jobOfferTitle,
                CompanyName = companyName,
                ContactHiringSection = contactHiringSection,
                Applicants = applicants,
                Description = descriptionText,
                SalaryOrBudgetOffered = salaryOrBudgetOffered,
                Link = url,
                SearchText = searchText
            };
        }

        private IWebElement ExtractHeader()
        {
            string xpathToFind = "//div[contains(@class, 't-14') and contains(@class, 'artdeco-card')]";
            var header = _driver.FindElements(By.XPath(xpathToFind)).FirstOrDefault();
            if (header == null)
            {
                var message = $"❌ ID:{_executionOptions.TimeStamp} Header not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            return header;
        }

        private IWebElement ExtractDetail()
        {
            var details = _driver.FindElements(By.XPath("//div[contains(@class, 'jobs-box--with-cta-large')]"));
            if (details.Count == 0)
            {
                var message = $"❌ Job details container not found. Current URL: {_driver.Url}";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }
            var detail = details.First();
            ClickSeeMore(detail);
            return detail;
        }

        private void ClickSeeMore(IWebElement detail)
        {
            try
            {
                string xpathToFind = ".//button[contains(@class, 'jobs-description__footer-button') and contains(., 'See more')]";
                var seeMoreButton = detail.FindElements(By.XPath(xpathToFind)).FirstOrDefault();
                if (seeMoreButton != null)
                {
                    seeMoreButton.Click();
                    _logger.LogDebug($"✅ ID:{_executionOptions.TimeStamp} 'See more' button clicked.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} Could not click 'See more' button: {ex.Message}");
            }
        }

        private string ExtractApplicants()
        {
            string xpathToFind = "//div[contains(@class, 'job-details-jobs-unified-top-card__primary-description-container')]";
            var applicants = _wait.Until(driver => driver.FindElements(By.XPath(xpathToFind)));
            return applicants.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
        private static string ExtractSalary(IWebElement detail)
        {
            IEnumerable<IWebElement> jobDetailsContainers(IWebElement scope) => scope.FindElements(By.CssSelector(".artdeco-card.job-details-module"));
            return jobDetailsContainers(detail)
                .SelectMany(c => c.FindElements(By.XPath(".//p[contains(., 'CA$')]")))
                .FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
        private static string ExtractDescription(IWebElement detail)
        {
            return detail.FindElements(By.CssSelector("article.jobs-description__container")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractContactHiring(IWebElement detail)
        {
            return detail.FindElements(By.CssSelector("div.job-details-module .jobs-poster__name strong")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractCompany(IWebElement header)
        {
            return header.FindElements(By.CssSelector(".job-details-jobs-unified-top-card__company-name a")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }

        private static string ExtractTitle(IWebElement header)
        {
            return header.FindElements(By.CssSelector("h1.t-24.t-bold.inline")).FirstOrDefault()?.Text?.Trim() ?? string.Empty;
        }
    }
}
