using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class DocumentCoordinator : IDocumentCoordinator
    {
        private readonly IJobStorageService _jobStorageService;
        private readonly IDocumentParse _documentParse;
        private readonly IGenerator _generator;
        private readonly IDocumentPDF _documentPDF;
        private readonly ExecutionOptions _executionOptions;
        private readonly IDirectoryCheck _directoryCheck;
        private readonly ILogger<DocumentCoordinator> _logger;
        private const string FolderName = "Document";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);

        public DocumentCoordinator(
            IJobStorageService jobStorageService,
            IDocumentParse documentParse,
            IGenerator generator,
            IDocumentPDF documentPDF,
            IDirectoryCheck directoryCheck,
            ExecutionOptions executionOptions,
            ILogger<DocumentCoordinator> logger)
        {
            _jobStorageService = jobStorageService;
            _documentParse = documentParse;
            _generator = generator;
            _documentPDF = documentPDF;
            _directoryCheck = directoryCheck;
            _executionOptions = executionOptions;
            _logger = logger;

            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _logger.LogInformation("📂 Document folder ensured at path: {FolderPath}", FolderPath);
        }

        public async Task GenerateDocumentAsync(string inputResume, string urlJobBoard)
        {
            _logger.LogInformation("🚀 Starting document generation process...");

            var listJobOfferDetail = await _jobStorageService.LoadJobsAsync();
            _logger.LogInformation("Loaded {JobCount} job offer(s) from storage.", listJobOfferDetail.Count());

            foreach (var jobOfferDetail in listJobOfferDetail)
            {
                _logger.LogInformation("Processing job offer ID: {JobID}", jobOfferDetail.ID);

                var jobOffer = await _documentParse.ParseJobOfferAsync(jobOfferDetail.Description);
                var resume = await _documentParse.ParseResumeAsync(inputResume);

                _logger.LogInformation("Generating customized resume and cover letter for job ID: {JobID}", jobOfferDetail.ID);
                var resumeModify = await _generator.CreateResume(jobOffer, resume);
                var coverLetter = await _generator.CreateCoverLetter(jobOffer, resume);

                var documentFolder = Path.Combine(_executionOptions.ExecutionFolder, jobOfferDetail.ID);
                _directoryCheck.EnsureDirectoryExists(documentFolder);
                _logger.LogInformation("Ensured document folder exists: {DocumentFolder}", documentFolder);

                var coverLetterRequest = new CoverLetterRequest
                {
                    UrlJobBoard = urlJobBoard,
                    JobOffer = jobOffer,
                    Resume = resumeModify,
                    CoverLetter = coverLetter
                };
                _documentPDF.GenerateCoverLetterPdf(documentFolder, coverLetterRequest);
                _logger.LogInformation("✅ Generated cover letter PDF for job ID: {JobID}", jobOfferDetail.ID);

                _documentPDF.GenerateJobOfferPdf(documentFolder, jobOffer);
                _logger.LogInformation("✅ Generated job offer PDF for job ID: {JobID}", jobOfferDetail.ID);

                var resumeRequest = new ResumeRequest
                {
                    UrlJobBoard = urlJobBoard,
                    JobOffer = jobOffer,
                    Resume = resumeModify
                };
                _documentPDF.GenerateResumePdf(documentFolder, resumeRequest);
                _logger.LogInformation("✅ Generated resume PDF for job ID: {JobID}", jobOfferDetail.ID);
            }

            _logger.LogInformation("🎯 Document generation process completed.");
        }
    }
}
