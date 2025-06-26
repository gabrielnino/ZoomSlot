using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Commands
{
    public class SearchCommand : ICommand
    {
        private readonly IJobSearchCoordinator _linkedInService;
        private readonly IDetailProcessing _detailProcessing;
        private readonly ILogger<SearchCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly AppConfig _config;
        private readonly ExecutionOptions _executionOptions;

        public SearchCommand(
            IJobSearchCoordinator linkedInService,
            ILogger<SearchCommand> logger,
            IJobStorageService storageService,
            IDetailProcessing detailProcessing,
            ExecutionOptions executionOptions,
            AppConfig config)
        {
            _linkedInService = linkedInService;
            _logger = logger;
            _storageService = storageService;
            _detailProcessing = detailProcessing;
            _config = config;
            _executionOptions = executionOptions;
        }
        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job search...");
            var job = await _linkedInService.SearchJobsAsync();
            var jobDetails = await _detailProcessing.ProcessOffersAsync(job, _config.JobSearch.SearchText);
            await _storageService.SaveJobOfferDetailAsync(_storageService.StorageFile, jobDetails);
            var execution = _executionOptions.ExecutionFolder;
            var completed = _executionOptions.CompletedFolder;
            RenameFolder(execution, completed);
        }
        public void RenameFolder(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                _logger?.LogWarning("❗ Source or destination path is empty or null.");
                throw new ArgumentException("Source path and new folder name must not be empty.");
            }

            if (!Directory.Exists(source))
            {
                _logger?.LogError("❌ Source folder not found: {Source}", source);
                throw new DirectoryNotFoundException($"Source folder not found: {source}");
            }

            if (!Directory.Exists(destination))
            {
                _logger?.LogInformation("📁 Destination folder does not exist. Creating folder: {Destination}", destination);
                Directory.CreateDirectory(destination);
            }

            _logger?.LogInformation("🔄 Renaming folder from {Source} to {Destination}", source, destination);
            try
            {
                Directory.Move(source, destination);
            }
            catch
            {

            }
            _logger?.LogInformation("✅ Folder successfully renamed.");
        }
    }

}
