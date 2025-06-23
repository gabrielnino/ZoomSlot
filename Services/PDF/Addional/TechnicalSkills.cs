using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Services.PDF.Addional
{
    internal static class TechnicalSkills
    {
        public static void AddTechnicalSkills(XGraphics gfx, XFont normalFont, List<string> lines, double margin, ref double yPosition, PdfPage pdfPage, double interlineParagraph)
        {
            foreach (var line in lines)
            {
                gfx.DrawString(line, normalFont, XBrushes.Black, new XRect(margin, yPosition, pdfPage.Width - 2 * margin, pdfPage.Height), XStringFormats.TopLeft);
                yPosition += interlineParagraph;
            }

            yPosition += 20;
        }
    }
}
