using Models;

namespace Services.Interfaces
{
    public interface IDocumentPDF
    {
        void GenerateCoverLetterPdf(string outputPath, CoverLetterRequest coverLetterRequest);
        void GenerateJobOfferPdf(string outputPath, JobOffer jobOffer);
        void GenerateResumePdf(string outputPath, ResumeRequest resumeRequest);
    }
}
