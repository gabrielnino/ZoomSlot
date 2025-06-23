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
        private const string FolderName = "Document";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);


        public DocumentCoordinator(
            IJobStorageService jobStorageService, 
            IDocumentParse documentParse, 
            IGenerator generator, 
            IDocumentPDF documentPDF,
            IDirectoryCheck directoryCheck,
            ExecutionOptions executionOptions)
        {
            _jobStorageService = jobStorageService;
            _documentParse = documentParse;
            _generator = generator;
            _documentPDF = documentPDF;
            _directoryCheck = directoryCheck;
            _executionOptions = executionOptions;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }
        public async Task GenerateDocumentAsync(string inputResume, string urlJobBoard)
        {
           var listJobOfferDetail = await _jobStorageService.LoadJobsAsync();
            foreach (var jobOfferDetail in listJobOfferDetail)
            {
                var jobOffer = await _documentParse.ParseJobOfferAsync(jobOfferDetail.Description);
                var resume = await _documentParse.ParseResumeAsync(inputResume);
                var resumeModify = await _generator.CreateResume(jobOffer, resume);
                var coverLetter = await _generator.CreateCoverLetter(jobOffer, resume);
                var baseFolder = _executionOptions.ExecutionFolder;
                var documentFolder = Path.Combine(baseFolder, jobOfferDetail.ID);
                _directoryCheck.EnsureDirectoryExists(documentFolder);
                var coverLetterRequest = new CoverLetterRequest
                {
                    UrlJobBoard = urlJobBoard,
                    JobOffer = jobOffer,
                    Resume = resumeModify,
                    CoverLetter = coverLetter
                };
                _documentPDF.GenerateCoverLetterPdf(documentFolder, coverLetterRequest);
                _documentPDF.GenerateJobOfferPdf(documentFolder, jobOffer);
                var resumeRequest = new ResumeRequest
                {
                    UrlJobBoard = urlJobBoard,
                    JobOffer = jobOffer,
                    Resume = resumeModify
                };
                _documentPDF.GenerateResumePdf(documentFolder, resumeRequest);
            }
        }
    }
}
