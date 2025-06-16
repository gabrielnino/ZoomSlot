using Microsoft.Extensions.Logging;
using Services;

namespace Commands
{
    public class SearchCommand : ICommand
    {
        private readonly ILinkedInService _linkedInService;
        private readonly ILogger<SearchCommand> _logger;

        public SearchCommand(ILinkedInService linkedInService, ILogger<SearchCommand> logger)
        {
            _linkedInService = linkedInService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting job search...");
            await _linkedInService.SearchJobsAsync();
            _logger.LogInformation("Job search completed successfully.");
        }
    }
}
