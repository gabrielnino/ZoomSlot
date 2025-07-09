using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Commands
{
    public class DetailCommand : ICommand
    {
        private readonly IDetailProcessing _detailProcessing;
        private readonly ILogger<DetailCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly AppConfig _config;
        private readonly ExecutionOptions _executionOptions;
        private string OffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, _config.FilePaths.SearchOutputFilePath);
        private string DetailOffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, _config.FilePaths.DetailOutputFilePath);
        public DetailCommand(
            ILogger<DetailCommand> logger,
            IJobStorageService storageService,
            IDetailProcessing detailProcessing,
            ExecutionOptions executionOptions,
            AppConfig config)
        {
            _logger = logger;
            _storageService = storageService;
            _detailProcessing = detailProcessing;
            _config = config;
            _executionOptions = executionOptions;
        }
        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job detailed...");
            var job = await _storageService.LoadJobsUrlAsync(OffersFilePath);
            var jobDetails = await _detailProcessing.ProcessOffersAsync(job, _config.JobSearch.SearchText);
            await _storageService.SaveJobOfferDetailAsync(DetailOffersFilePath, jobDetails);
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
            _logger?.LogInformation("✅ Folder successfully renamed.");
        }
    }

}
