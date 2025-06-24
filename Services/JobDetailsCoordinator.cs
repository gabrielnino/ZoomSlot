using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using Services.Interfaces;
using System.Text.Json;

namespace Services
{
    public class JobDetailsCoordinator : IJobDetailsCoordinator, IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly ILogger<JobSearchCoordinator> _logger;
        private bool _disposed = false;
        private readonly ILoginService _loginService;
        private readonly ExecutionOptions _executionOptions;
        private readonly ICaptureSnapshot _capture;
        private List<string>? _offers;
        private List<JobOfferDetail>? _offersDetail;
        private readonly IDetailProcessing _jobOfferDetail;
        private readonly IDirectoryCheck _directoryCheck;
        //IDetailProcessing, DetailProcessing
        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers.json");

        public JobDetailsCoordinator(
            IWebDriverFactory driverFactory,
            ILogger<JobSearchCoordinator> logger,
            ILoginService loginService,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            IDetailProcessing jobOfferDetail,
            IDirectoryCheck directoryCheck)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(_executionOptions.ExecutionFolder);
            _loginService = loginService;
            _capture = capture;
            _jobOfferDetail = jobOfferDetail;
        }

        public async Task<List<JobOfferDetail>> DetailJobsAsync(List<string> job, string searchText)
        {
            try
            {
                _logger.LogInformation($"🚀 ID:{_executionOptions.TimeStamp} Starting LinkedIn job search process...");
                await _loginService.LoginAsync();
                _offersDetail = await _jobOfferDetail.ProcessOffersAsync(job, searchText);
                await SaveOffersAsync();
                return _offersDetail ?? [];
            }
            catch (Exception ex)
            {
                var timestamp = await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "An unexpected error");
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} An unexpected error occurred. Debug artifacts saved at {timestamp}");
                throw new ApplicationException("Job search failed.", ex);
            }
        }

        private async Task SaveOffersAsync()
        {
            if (_offers == null || !_offers.Any())
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} No offers to save.");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(_offers, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(OffersFilePath, json);
                _logger.LogInformation($"💾 ID:{_executionOptions.TimeStamp} Saved {_offers.Count} offers to: {OffersFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Failed to save offers to file.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger.LogDebug($" ID:{_executionOptions.TimeStamp}🧹 Disposing browser driver and cleaning resources...");
                _driver?.Quit();
                _driver?.Dispose();
                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Browser driver and resources disposed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Exception encountered while disposing browser resources.");
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~JobDetailsCoordinator()
        {
            Dispose();
        }
    }
}
