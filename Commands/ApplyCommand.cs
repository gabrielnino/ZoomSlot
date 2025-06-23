using Microsoft.Extensions.Logging;
using Services;
using Services.Interfaces;

namespace Commands
{
    public class ApplyCommand : ICommand
    {
        private readonly IJobSearchCoordinator _linkedInService;
        private readonly ILogger<SearchCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly IDocumentCoordinator _documentCoordinator;

        public ApplyCommand(ILogger<SearchCommand> logger, IDocumentCoordinator documentCoordinator)
        {
            _logger = logger;
            _documentCoordinator = documentCoordinator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job search...");
            var jobDetails = await _linkedInService.SearchJobsAsync();
            if (jobDetails != null && jobDetails.Any())
            {
                string inputResume = arguments["--apply"];
                string urlJobBoard = arguments["--urljobboard"];
                await _documentCoordinator.GenerateDocumentAsync(inputResume, urlJobBoard);

                _logger.LogInformation("✅ Job search completed and job details saved.");
            }
            else
            {
                _logger.LogWarning("⚠️ No job details found to save.");
            }
        }
    }

}
