using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class ResumeCommand : ICommand
    {
        private readonly ILogger<ResumeCommand> _logger;
        private readonly IResumeStorageService _storageService;
        private readonly IResumeDocumentCoordinator _resumeDocumentCoordinator;

        public ResumeCommand(
            ILogger<ResumeCommand> logger,
            IResumeStorageService storageService,
            IResumeDocumentCoordinator resumeDocumentCoordinator)
        {
            _logger = logger;
            _storageService = storageService;
            _resumeDocumentCoordinator = resumeDocumentCoordinator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            string resumePath = arguments?.GetValueOrDefault("--resume", string.Empty) ?? string.Empty;
            string resumeText = await _storageService.LoadResumeAsync(resumePath);
            var resume = await _resumeDocumentCoordinator.GenerateResumeDocumentAsync(resumeText);
            await _storageService.SaveResumeAsync(_storageService.StorageFile, resume);
        }

    }
}
