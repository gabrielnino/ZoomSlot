namespace Services.PDF.Addional
{
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;

    internal static class ProfessionalSummary
    {
        public static void AddProfessionalSummary(XGraphics gfx, XFont normalFont, List<string> lines, double margin, ref double yPosition, PdfPage pdfPage, double interlineParagraph)
        {
            foreach (var line in lines)
            {
                gfx.DrawString(line, normalFont, XBrushes.Black, new XRect(margin, yPosition, pdfPage.Width - 2 * margin, pdfPage.Height), XStringFormats.TopLeft);
                yPosition += interlineParagraph;
            }

            yPosition += 30;
        }
    }
}
