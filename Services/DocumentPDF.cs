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
            CoverLetterPdf.Generate(outputPath, coverLetterRequest);
            _logger.LogInformation("✅ Cover letter PDF generated successfully at: {OutputPath}", outputPath);
        }

        public void GenerateJobOfferPdf(string outputPath, JobOffer jobOffer)
        {
            _logger.LogInformation("📝 Generating job offer PDF at: {OutputPath}", outputPath);
            JobOfferPdf.Generate(outputPath, jobOffer);
            _logger.LogInformation("✅ Job offer PDF generated successfully at: {OutputPath}", outputPath);
        }

        public void GenerateResumePdf(string outputPath, ResumeRequest resumeRequest)
        {
            _logger.LogInformation("📝 Generating resume PDF at: {OutputPath}", outputPath);
            ResumePdf.Generate(outputPath, resumeRequest);
            _logger.LogInformation("✅ Resume PDF generated successfully at: {OutputPath}", outputPath);
        }
    }
}
