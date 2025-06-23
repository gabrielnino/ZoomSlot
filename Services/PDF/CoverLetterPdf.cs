namespace ApplyEngine.JobOfferApply.Documents.PDF
{
    using Models;
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;
    using Services.PDF.Addional;

    public static class CoverLetterPdf
    {
        public static void Generate(string outputPath, CoverLetterRequest coverLetterRequest)
        {
            const string familyName = "Verdana";
            var document = new PdfDocument();
            PdfPage page;
            XGraphics gfx;
            double margin, interline, yPosition;
            XFont normalFont, subTitleFont;
            string qualifications;
            PdfHeaderBuilder.GetHeader(coverLetterRequest.JobOffer, coverLetterRequest.Resume, familyName, document, out page, out gfx, out margin, out interline, out yPosition, out normalFont, out subTitleFont, coverLetterRequest.UrlJobBoard);
            var contentLines = TextHelper.SplitText(coverLetterRequest.CoverLetter.ProfessionalSummary, 90);
            yPosition += 40;
            var saidHello = "Dear Hiring Manager,";
            gfx.DrawString(saidHello, normalFont, XBrushes.Black, margin, yPosition);
            yPosition += interline;
            foreach (var line in contentLines.Where(x => x != string.Empty).ToList())
            {
                gfx.DrawString(line, normalFont, XBrushes.Black, margin, yPosition);
                yPosition += interline;
            }

            yPosition += 10;
            foreach (var bullet in coverLetterRequest.CoverLetter.BulletPoints)
            {
                var lines = TextHelper.SplitText(bullet, 85);
                var first = lines.First();
                gfx.DrawString($"• {first}", normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, page.Width - 2 * margin, page.Width), XStringFormats.TopLeft);
                yPosition += 10;
                foreach (var line in lines.Skip(1))
                {
                    gfx.DrawString(line, normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, page.Width - 2 * margin, page.Width), XStringFormats.TopLeft);
                    yPosition += 10;
                }

                yPosition += 10;
            }

            yPosition += 40;
            var footerLines = TextHelper.SplitText(coverLetterRequest.CoverLetter.ClosingParagraph, 90);
            foreach (var line in footerLines)
            {
                gfx.DrawString(line, normalFont, XBrushes.Black, margin, yPosition);
                yPosition += interline;
            }

            yPosition += interline;
            yPosition += interline;
            gfx.DrawString(coverLetterRequest.Resume.Name, normalFont, XBrushes.Black, margin, yPosition);
            yPosition += interline;
            gfx.DrawString(coverLetterRequest.JobOffer.JobOfferTitle, normalFont, XBrushes.Black, margin, yPosition);
            string date = DateTime.Now.ToString("MMMM d, yyyy");
            yPosition += interline;
            gfx.DrawString(date, normalFont, XBrushes.Black, margin, yPosition);
            document.Save(outputPath);
        }
    }
}