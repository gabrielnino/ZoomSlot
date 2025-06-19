using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class JobSearchCoordinator : IJobSearchCoordinator, IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly ILogger<JobSearchCoordinator> _logger;
        private bool _disposed = false;
        private readonly ILoginService _loginService;
        private readonly ExecutionOptions _executionOptions;
        private readonly ICaptureSnapshot _capture;
        private List<string>? _offers;
        private readonly IJobOfferDetail _jobOfferDetail;
        private readonly IJobSearch _searchService;
        private readonly IPageProcessor _processService;


        public JobSearchCoordinator(
            IWebDriverFactory driverFactory,
            ILogger<JobSearchCoordinator> logger,
            ILoginService loginService,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            IJobOfferDetail jobOfferDetail,
            IJobSearch searchService,
            IPageProcessor processService)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _executionOptions = executionOptions;
            EnsureDirectoryExists(_executionOptions.ExecutionFolder);
            _loginService = loginService;
            _capture = capture;
            _jobOfferDetail = jobOfferDetail;
            _searchService = searchService;
            _processService = processService;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation($"📁 Created execution folder at: {_executionOptions.ExecutionFolder}");
            }
        }

        public async Task SearchJobsAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Starting LinkedIn job search process...");
                await _loginService.LoginAsync();
                await _searchService.PerformSearchAsync();
                _offers = await _processService.ProcessAllPagesAsync();
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

        ~JobSearchCoordinator()
        {
            Dispose();
        }
    }
}
