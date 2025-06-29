using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class PromtCommand : ICommand
    {

        private readonly ILogger<PromtCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly IPromptGenerator _promptGenerator;

        public PromtCommand(
            ILogger<PromtCommand> logger,
            IPromptGenerator promptGenerator)
        {
            _logger = logger;
            _promptGenerator = promptGenerator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job application process...");
            //var filePath = _storageService.StorageFile;
            //var jobDetails = await _storageService.LoadJobsDetailAsync(filePath);
            //_logger.LogInformation("Found {JobCount} job(s) to apply for.", jobDetails.Count());
            await _promptGenerator.ExecuteChain();
            //    if (jobDetails != null && jobDetails.Any())
            //    {
            //        foreach (var job in jobDetails)
            //        {
            //            _logger.LogInformation("Found job: {JobTitle}", job);
            //            if (arguments == null || !arguments.TryGetValue("--apply", out string? resumeFilePath))
            //            {
            //                _logger.LogError("❌ '--apply' argument is missing.");
            //                throw new ArgumentException("'--apply' argument is required to specify the resume file path.");
            //            }

            //            string urlJobBoard = arguments.GetValueOrDefault("--urljobboard", string.Empty);
            //            string inputResumeContent;
            //            try
            //            {
            //                _logger.LogInformation("Reading resume file from path: {ResumeFilePath}", resumeFilePath);
            //                inputResumeContent = await File.ReadAllTextAsync(resumeFilePath);
            //                _logger.LogInformation("✅ Resume file read successfully.");
            //            }
            //            catch (Exception ex)
            //            {
            //                _logger.LogError(ex, "❌ Failed to read resume file at: {ResumeFilePath}", resumeFilePath);
            //                throw new IOException($"Error reading resume file at {resumeFilePath}", ex);
            //            }
            //            _logger.LogInformation("Generating application document...");
            //            await _documentCoordinator.GenerateDocumentAsync(inputResumeContent, urlJobBoard);
            //            _logger.LogInformation("✅ Application document generated successfully.");
            //            await _storageService.SaveJobOfferDetailAsync(_storageService.StorageFile, jobDetails);
            //        }
            //    }
            //    else
            //    {
            //        _logger.LogWarning("⚠️ No job details found to apply for.");
            //    }
            //}
        }

    }
}
