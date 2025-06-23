using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplyEngine.JobOfferApply.Documents.PDF;
using Models;
using Services.Interfaces;
using Services.PDF;

namespace Services
{
    public class DocumentPDF : IDocumentPDF
    {
        public void GenerateCoverLetterPdf(string outputPath, CoverLetterRequest coverLetterRequest)
        {
            CoverLetterPdf.Generate(outputPath, coverLetterRequest);
        }

        public void GenerateJobOfferPdf(string outputPath, JobOffer jobOffer)
        {
            JobOfferPdf.Generate(outputPath, jobOffer);
        }

        public void GenerateResumePdf(string outputPath, ResumeRequest resumeRequest)
        {
           ResumePdf.Generate(outputPath, resumeRequest);
        }
    }
}
