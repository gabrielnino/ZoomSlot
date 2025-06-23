using Microsoft.Extensions.Logging;
using Services;
using Services.Interfaces;

namespace Commands
{
    public class ApplyCommand : ICommand
    {
        private readonly IJobSearchCoordinator _linkedInService;
        private readonly ILogger<ApplyCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly IDocumentCoordinator _documentCoordinator;

        public ApplyCommand(
            ILogger<ApplyCommand> logger,
            IJobSearchCoordinator linkedInService,
            IJobStorageService storageService,
            IDocumentCoordinator documentCoordinator)
        {
            _logger = logger;
            _linkedInService = linkedInService;
            _storageService = storageService;
            _documentCoordinator = documentCoordinator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job application process...");

            var jobDetails = await _linkedInService.SearchJobsAsync();
            if (jobDetails != null && jobDetails.Any())
            {
                _logger.LogInformation("Found {JobCount} job(s) to apply for.", jobDetails.Count);

                if (arguments == null || !arguments.ContainsKey("--apply"))
                {
                    _logger.LogError("❌ '--apply' argument is missing.");
                    throw new ArgumentException("'--apply' argument is required to specify the resume file path.");
                }

                string resumeFilePath = arguments["--apply"];
                string urlJobBoard = arguments.GetValueOrDefault("--urljobboard", string.Empty);

                string inputResumeContent;
                try
                {
                    _logger.LogInformation("Reading resume file from path: {ResumeFilePath}", resumeFilePath);
                    inputResumeContent = await File.ReadAllTextAsync(resumeFilePath);
                    _logger.LogInformation("✅ Resume file read successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to read resume file at: {ResumeFilePath}", resumeFilePath);
                    throw new IOException($"Error reading resume file at {resumeFilePath}", ex);
                }

                _logger.LogInformation("Generating application document...");
                await _documentCoordinator.GenerateDocumentAsync(inputResumeContent, urlJobBoard);
                _logger.LogInformation("✅ Application document generated successfully.");

                await _storageService.SaveJobsAsync(jobDetails);
                _logger.LogInformation("✅ Job details saved successfully.");
            }
            else
            {
                _logger.LogWarning("⚠️ No job details found to apply for.");
            }
        }

    }
}
