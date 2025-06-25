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
            _logger.LogInformation("📁 Document directory ensured at: {FolderPath}", FolderPath);
        }

        public async Task GenerateDocumentAsync(string inputResume, string urlJobBoard)
        {
            _logger.LogInformation("🚀 Starting document generation process | Input resume: {InputResumeLength} chars | Job board URL: {UrlJobBoard}",
                inputResume.Length, urlJobBoard);

            try
            {
                var filePath = _jobStorageService.StorageFile;
                var listJobOfferDetail = await _jobStorageService.LoadJobsDetailAsync(filePath);
                _logger.LogInformation("📊 Loaded {JobCount} job offers from storage", listJobOfferDetail.Count());

                if (!listJobOfferDetail.Any())
                {
                    _logger.LogWarning("⚠️ No job offers found in storage - nothing to process");
                    return;
                }

                foreach (var jobOfferDetail in listJobOfferDetail)
                {
                    string iD = jobOfferDetail.ID;
                    string title = jobOfferDetail.JobOfferTitle;
                    _logger.LogInformation("🔍 Processing job offer | ID: {JobID} | Title: {JobTitle}", iD, title);

                    try
                    {
                        // Parse job offer and resume
                        var jobOffer = await _documentParse.ParseJobOfferAsync(jobOfferDetail.Description);
                        jobOffer.Url = jobOfferDetail.Link;
                        jobOffer.RawJobDescription = jobOfferDetail.Description.Split(Environment.NewLine);
                        jobOffer.Description = jobOfferDetail.Description;
                        _logger.LogDebug("📝 Parsed job offer | ID: {JobID}", jobOfferDetail.ID);
                        var resume = await _documentParse.ParseResumeAsync(inputResume);
                        _logger.LogDebug("📄 Parsed resume | Length: {ResumeLength} chars", inputResume.Length);

                        // Generate documents
                        _logger.LogInformation("🛠️ Generating customized documents for job ID: {JobID}", jobOfferDetail.ID);

                        var resumeModify = await _generator.CreateResume(jobOffer, resume);
                        var coverLetter = await _generator.CreateCoverLetter(jobOffer, resume);

                        var documentFolder = Path.Combine(FolderPath, jobOfferDetail.ID);
                        _directoryCheck.EnsureDirectoryExists(documentFolder);
                        _logger.LogDebug("📂 Created document folder: {DocumentFolder}", documentFolder);

                        // Generate PDFs
                        var coverLetterRequest = new CoverLetterRequest
                        {
                            UrlJobBoard = urlJobBoard,
                            JobOffer = jobOffer,
                            Resume = resumeModify,
                            CoverLetter = coverLetter
                        };

                        _documentPDF.GenerateCoverLetterPdf(documentFolder, coverLetterRequest);
                        _logger.LogInformation("✅ Generated cover letter | Job ID: {JobID} | Path: {DocumentFolder}",
                            jobOfferDetail.ID, documentFolder);

                        _documentPDF.GenerateJobOfferPdf(documentFolder, jobOffer);
                        _logger.LogInformation("✅ Generated job offer PDF | Job ID: {JobID}", jobOfferDetail.ID);

                        var resumeRequest = new ResumeRequest
                        {
                            UrlJobBoard = urlJobBoard,
                            JobOffer = jobOffer,
                            Resume = resumeModify
                        };

                        _documentPDF.GenerateResumePdf(documentFolder, resumeRequest);
                        _logger.LogInformation("✅ Generated resume PDF | Job ID: {JobID}", jobOfferDetail.ID);
                    }
                    catch (Exception ex)
                    {
                        string id = jobOfferDetail.ID;
                        string message = ex.Message;
                        _logger.LogError(ex, "❌ Error processing job offer ID: {JobID} | Error: {ErrorMessage}", id, message);
                        throw; // Re-throw to maintain original behavior
                    }
                }

                _logger.LogInformation("🎉 Successfully completed document generation for {JobCount} job offers", listJobOfferDetail.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Document generation process failed | Error: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}