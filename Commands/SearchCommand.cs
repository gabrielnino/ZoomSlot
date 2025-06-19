using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class SearchCommand : ICommand
    {
        private readonly IJobSearchCoordinator _linkedInService;
        private readonly ILogger<SearchCommand> _logger;
        private readonly IJobStorageService _storageService;

        public SearchCommand(IJobSearchCoordinator linkedInService, ILogger<SearchCommand> logger, IJobStorageService storageService)
        {
            _linkedInService = linkedInService;
            _logger = logger;
            _storageService = storageService;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting job search...");
            var jobDetails = await _linkedInService.SearchJobsAsync();
            if (jobDetails != null && jobDetails.Any())
            {
                await _storageService.SaveJobsAsync(jobDetails);
                _logger.LogInformation("✅ Job search completed and job details saved.");
            }
            else
            {
                _logger.LogWarning("⚠️ No job details found to save.");
            }
        }
    }

}
