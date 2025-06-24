using ApplyEngine.JobOfferApply.Documents.PDF;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;
using Services.PDF;

namespace Services
{
    public class DocumentPDF : IDocumentPDF
    {
        private readonly ILogger<DocumentPDF> _logger;

        public DocumentPDF(ILogger<DocumentPDF> logger)
        {
            _logger = logger;
        }

        public void GenerateCoverLetterPdf(string outputPath, CoverLetterRequest coverLetterRequest)
        {
            _logger.LogInformation("📝 Generating cover letter PDF at: {OutputPath}", outputPath);

            var jobTitle = StringHelpers.NormalizeCompanyName(coverLetterRequest.JobOffer.JobOfferTitle);
            var companyName = StringHelpers.NormalizeCompanyName(coverLetterRequest.JobOffer.CompanyName);
            var fileNameCoverLetter = $"{companyName}_covertLetter_{jobTitle}.pdf";
            var fileNameJobOfferPath = Path.Combine(outputPath, fileNameCoverLetter);

            CoverLetterPdf.Generate(fileNameJobOfferPath, coverLetterRequest);
            _logger.LogInformation("✅ Cover letter PDF generated successfully at: {OutputPath}", outputPath);
        }

        public void GenerateJobOfferPdf(string outputPath, JobOffer jobOffer)
        {
            _logger.LogInformation("📝 Generating job offer PDF at: {OutputPath}", outputPath);
            var jobTitle = StringHelpers.NormalizeCompanyName(jobOffer.JobOfferTitle);
            var companyName = StringHelpers.NormalizeCompanyName(jobOffer.CompanyName);
            var fileNameCoverLetter = $"{companyName}_JobOffer_{jobTitle}.pdf";
            var fileNameJobOfferPath = Path.Combine(outputPath, fileNameCoverLetter);
            JobOfferPdf.Generate(fileNameJobOfferPath, jobOffer);
            _logger.LogInformation("✅ Job offer PDF generated successfully at: {OutputPath}", outputPath);
        }

        public void GenerateResumePdf(string outputPath, ResumeRequest resumeRequest)
        {
            _logger.LogInformation("📝 Generating resume PDF at: {OutputPath}", outputPath);
            var jobTitle = StringHelpers.NormalizeCompanyName(resumeRequest.JobOffer.JobOfferTitle);
            var companyName = StringHelpers.NormalizeCompanyName(resumeRequest.JobOffer.CompanyName);
            var fileNameCoverLetter = $"{companyName}_resume_{jobTitle}.pdf";
            var fileNameJobOfferPath = Path.Combine(outputPath, fileNameCoverLetter);
            ResumePdf.Generate(fileNameJobOfferPath, resumeRequest);
            _logger.LogInformation("✅ Resume PDF generated successfully at: {OutputPath}", outputPath);
        }
    }
}
