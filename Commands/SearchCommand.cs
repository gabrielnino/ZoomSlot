﻿using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Commands
{
    public class SearchCommand(
        IJobSearchCoordinator linkedInService,
        ILogger<SearchCommand> logger) : ICommand
    {
        private readonly IJobSearchCoordinator _linkedInService = linkedInService;
        private readonly ILogger<SearchCommand> _logger = logger;

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job search...");
            await _linkedInService.SearchJobsAsync();
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
