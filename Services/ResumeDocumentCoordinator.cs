using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class ResumeDocumentCoordinator : IResumeDocumentCoordinator
    {

        private readonly IDocumentParse _documentParse;
        private readonly ExecutionOptions _executionOptions;
        private readonly IDirectoryCheck _directoryCheck;
        private readonly ILogger<JobDocumentCoordinator> _logger;
        private const string FolderName = "Document";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);

        public ResumeDocumentCoordinator(
            IDocumentParse documentParse,
            ILogger<JobDocumentCoordinator> logger,
            IDirectoryCheck directoryCheck,
            ExecutionOptions executionOptions)
        {
            _documentParse = documentParse;
            _logger = logger;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _logger.LogInformation("📁 Document directory ensured at: {FolderPath}", FolderPath);
        }

        public async Task<Resume> GenerateResumeDocumentAsync(string resumeText)
        {
            var parsedResume = await _documentParse.ParseResumeAsync(resumeText);
            return parsedResume;
        }
    }
}
