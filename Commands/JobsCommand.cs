using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class JobsCommand : ICommand
    {
        private readonly ILogger<ApplyCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly IJobDocumentCoordinator _jobDocumentCoordinator;

        public JobsCommand(
            ILogger<ApplyCommand> logger,
            IJobSearchCoordinator linkedInService,
            IJobStorageService storageService,
            IJobDocumentCoordinator jobDocumentCoordinator)
        {
            _logger = logger;
            _storageService = storageService;
            _jobDocumentCoordinator = jobDocumentCoordinator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            var offers = await _jobDocumentCoordinator.GenerateJobsDocumentAsync();
            _logger.LogInformation("✅ Application document generated successfully.");
            await _storageService.SaveJobOfferAsync(_storageService.StorageFile, offers);
            _logger.LogInformation("✅ Application document finished successfully.");
        }

    }
}
