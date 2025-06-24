using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using OpenQA.Selenium;
using Services.Interfaces;

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
        private readonly IJobSearch _searchService;
        private readonly IPageProcessor _processService;
        private readonly IDirectoryCheck _directoryCheck;

        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, "offers.json");

        public JobSearchCoordinator(
            IWebDriverFactory driverFactory,
            ILogger<JobSearchCoordinator> logger,
            ILoginService loginService,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            IJobSearch searchService,
            IPageProcessor processService,
            IDirectoryCheck directoryCheck)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(_executionOptions.ExecutionFolder);
            _loginService = loginService;
            _capture = capture;
            _searchService = searchService;
            _processService = processService;
        }

        public async Task<List<string>> SearchJobsAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(OffersFilePath) && File.Exists(OffersFilePath))
                {
                    _offers = await LoadOffersAsync(OffersFilePath);
                    if (_offers != null && _offers.Any())
                    {
                        _logger.LogInformation($"💼 ID:{_executionOptions.TimeStamp} Loaded {_offers.Count} offers fro{OffersFilePath}.");
                        return _offers;
                    }
                }
                _logger.LogInformation($"🚀 ID:{_executionOptions.TimeStamp} Starting LinkedIn job search process...");
                await _loginService.LoginAsync();
                var searchText = await _searchService.PerformSearchAsync();
                _offers = await _processService.ProcessAllPagesAsync();
                await SaveOffersAsync(_offers);
                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} LinkedIn job search process completed successfully with {_offers?.Count ?? 0} offers found.");
                return _offers ?? [];
            }
            catch (Exception ex)
            {
                var timestamp = await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "An unexpected error");
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} An unexpected error occurred during job search. Debug artifacts saved at {timestamp}");
                throw new ApplicationException("Job search process failed. See inner exception for details.", ex);
            }
        }



        private async Task<List<string>>? LoadOffersAsync(string offersFilePath)
        {
            var json = await File.ReadAllTextAsync(offersFilePath);
            var offers = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            return offers;
        }

        private async Task SaveOffersAsync(List<string> offers)
        {
            if (offers == null || !offers.Any())
            {
                _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} No offers to save.");
                return;
            }

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(offers, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(OffersFilePath, json);
                _logger.LogInformation($"💾 ID:{_executionOptions.TimeStamp} Saved {offers.Count} offers to: {OffersFilePath}");
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

        ~JobSearchCoordinator()
        {
            Dispose();
        }
    }
}
