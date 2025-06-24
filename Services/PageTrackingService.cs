using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class PageTrackingService : IPageTrackingService
    {
        private readonly ILogger<PageTrackingService> _logger;
        private readonly ExecutionOptions _executionOptions;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public PageTrackingService(ILogger<PageTrackingService> logger, ExecutionOptions executionOptions)
        {
            _logger = logger;
            _executionOptions = executionOptions;
        }

        private string GetTrackingFilePath(string searchId)
            => Path.Combine(_executionOptions.ExecutionFolder, $"page_tracking_{searchId}.json");

        public async Task<PageTrackingState> LoadPageStateAsync(string searchId)
        {
            await _fileLock.WaitAsync();
            try
            {
                var filePath = GetTrackingFilePath(searchId);
                if (!File.Exists(filePath)) return new PageTrackingState();

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<PageTrackingState>(json) ?? new PageTrackingState();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SavePageStateAsync(string searchId, PageTrackingState state)
        {
            await _fileLock.WaitAsync();
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(state, options);
                await File.WriteAllTextAsync(GetTrackingFilePath(searchId), json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task ClearPageStateAsync(string searchId)
        {
            await _fileLock.WaitAsync();
            try
            {
                var filePath = GetTrackingFilePath(searchId);
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
