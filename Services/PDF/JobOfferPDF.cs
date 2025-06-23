using Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Services.PDF
{
    public static class JobOfferPdf
    {
        public static void Generate(string outputPath, JobOffer jobOffer)
        {
            const string familyName = "Verdana";
            const int maxLinesPerPage = 36; // Maximum lines per page
            const double margin = 50;
            const double interline = 20;
            var document = new PdfDocument();

            // Fonts
            var normalFont = new XFont(familyName, 10, XFontStyleEx.Regular);
            var subTitleFont = new XFont(familyName, 12, XFontStyleEx.Bold);

            // Prepare job offer content
            var essentialQualifications = jobOffer.EssentialTechnicalSkillQualifications.Take(6).ToList();
            var additionalQualificationsNeeded = 6 - essentialQualifications.Count;
            if (additionalQualificationsNeeded > 0)
            {
                essentialQualifications.AddRange(
                    jobOffer.OtherTechnicalSkillQualifications.Take(additionalQualificationsNeeded)
                );
            }

            // Document info
            document.Info.Title = jobOffer.JobOfferTitle;

            // Job description lines
            var lines = jobOffer.RawJobDescription
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // Generate pages
            PdfPage page = null;
            XGraphics gfx = null;
            double yPosition = 0;
            int currentLineCount = 0;

            // Helper to start a new page
            void StartNewPage()
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPosition = margin; // Reset y-position for a new page
                currentLineCount = 0;
            }

            // Start the first page
            StartNewPage();

            // Draw the title
            gfx.DrawString(jobOffer.JobOfferTitle, subTitleFont, XBrushes.Black, margin, yPosition);
            yPosition += interline;
            currentLineCount++;

            // Draw the content
            foreach (var line in lines)
            {
                if (currentLineCount >= maxLinesPerPage)
                {
                    StartNewPage();
                }

                gfx.DrawString(line, normalFont, XBrushes.Black, margin, yPosition);
                yPosition += interline;
                currentLineCount++;
            }

            // Save the document
            document.Save(outputPath);
        }
    }
}