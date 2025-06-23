namespace Services.PDF.Addional
{
    using PdfSharp.Drawing;

    public static class AddionalQualitions
    {
        public static void AddAddionalQualitions(XGraphics gfx, string[] bullets, double margin, ref double yPosition, double pageWidth)
        {
            XFont boldFont = new XFont("Verdana", 10, XFontStyleEx.Bold);
            XFont normalFont = new XFont("Verdana", 10, XFontStyleEx.Regular);
            yPosition += 10;
            foreach (var bullet in bullets)
            {
                var buletLines = TextHelper.SplitText(bullet, 70);
                var bulletfirts = buletLines.First();
                var buletLinesLast = buletLines.Skip(1);
                gfx.DrawString($"• {bullet}", normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, pageWidth - 2 * margin, pageWidth), XStringFormats.TopLeft);
                yPosition += 10;
                foreach (var bulletLine in buletLinesLast)
                {
                    gfx.DrawString($"{bulletLine}", normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, pageWidth - 2 * margin, pageWidth), XStringFormats.TopLeft);
                    yPosition += 10;
                }
            }
            yPosition += 20;
        }
    }
}