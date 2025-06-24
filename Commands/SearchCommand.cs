using Configuration;
using Microsoft.Extensions.Logging;
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

        public SearchCommand(
            IJobSearchCoordinator linkedInService, 
            ILogger<SearchCommand> logger, 
            IJobStorageService storageService,
            IDetailProcessing detailProcessing,
            AppConfig config)
        {
            _linkedInService = linkedInService;
            _logger = logger;
            _storageService = storageService;
            _detailProcessing = detailProcessing;
            _config = config;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job search...");
            var job = await _linkedInService.SearchJobsAsync();
            var jobDetails = await _detailProcessing.ProcessOffersAsync(job, _config.JobSearch.SearchText);
            await _storageService.SaveJobOfferDetailAsync(jobDetails);
        }
    }

}
