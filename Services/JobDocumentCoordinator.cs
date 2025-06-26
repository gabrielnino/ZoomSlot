using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class JobDocumentCoordinator : IJobDocumentCoordinator
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

        public JobDocumentCoordinator(
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

        public async Task<IEnumerable<JobOffer>> GenerateJobsDocumentAsync()
        {
            _logger.LogInformation("🚀 Starting document generation process | Input resume: {InputResumeLength} chars | Job board URL: {UrlJobBoard}");

            try
            {
                var filePath = Path.Combine(_executionOptions.ExecutionFolder, _jobStorageService.StorageFile);
                var listJobOfferDetail = await _jobStorageService.LoadJobsDetailAsync(filePath);
                _logger.LogInformation("📊 Loaded {JobCount} job offers from storage", listJobOfferDetail.Count());

                if (!listJobOfferDetail.Any())
                {
                    _logger.LogWarning("⚠️ No job offers found in storage - nothing to process");
                    return [];
                }
                var jobOffers = new List<JobOffer>();
                foreach (var jobOfferDetail in listJobOfferDetail)
                {
                    string iD = jobOfferDetail.ID;
                    string title = jobOfferDetail.JobOfferTitle;
                    _logger.LogInformation("🔍 Processing job offer | ID: {JobID} | Title: {JobTitle}", iD, title);

                    try
                    {
                        // Parse job offer and resume
                        var jobOffer = await _documentParse.ParseJobOfferAsync(jobOfferDetail.Description);
                        jobOffer.Description = jobOfferDetail.Description;
                        jobOffer.RawJobDescription = jobOfferDetail.Description.Split(Environment.NewLine).Distinct().ToList();
                        jobOffers.Add(jobOffer);
                    }
                    catch (Exception ex)
                    {
                        string id = jobOfferDetail.ID;
                        string message = ex.Message;
                        _logger.LogError(ex, "❌ Error processing job offer ID: {JobID} | Error: {ErrorMessage}", id, message);
                        throw; // Re-throw to maintain original behavior
                    }
                }
                return jobOffers;
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