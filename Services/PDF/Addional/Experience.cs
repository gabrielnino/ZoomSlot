namespace Services.PDF.Addional
{
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;
    using System;

    internal static class Experience
    {

        public static void AddExperience(XGraphics gfx, string title, string company, string location, string dates, string[] bullets, double margin, ref double yPosition, double pageWidth, string[] techStack, ref PdfPage page)
        {
            var urlCompanies = new Dictionary<string, string>
            {
                { "Snowflake", "https://www.snowflake.com/en/" },
                { "MAS Global", "https://masglobalconsulting.com/" },
                { "Software One", "https://www.softwareone.com/en-ca" },
                { "LeaderSearch", "https://www.elempleo.com/co/ofertas-empleo/" },
                { "Focus Communications INC", "https://focusd.ca/" },
                { "Just Go To Canada", "https://justgotocanada.com/en/" },
                { "CALIDDA - Gas Natural del Perú", "https://www.calidda.com.pe/" },
                { "VENTURA SOLUCIONES SAC", "https://venturasoluciones.com.pe/" },
                { "GOOGLE USA", "https://www.google.com/" },
                { "CRP MEDIOS Y ENTRETENIMIENTO SAC", "https://www.linkedin.com/company/corporaci-n-radial-del-per-/about/" },
                { "SAN MIGUEL INDUSTRIAS PET S.A", "https://www.linkedin.com/company/smipet/?originalSubdomain=pe" },
                { "INNIVEC", "https://innivec.com/" },
                { "INVERHAY", "http://www.lugano.com.co/" },
                { "MR BRANDING", "https://mrbranding.co/" },


                { "NAPA AUTOPRO", "https://www.napaautopro.com/en/" },
                { "The Salvation Army", "https://salvationarmy.ca/" },
                { "Car Dealer Hyundai", "https://hyundai.pe/" },
                { "Credit Bank of Peru", "https://www.viabcp.com/" },
                { "Car Dealer Kia", "https://www.kia.com/pe/main.html" },
                { "Car Dealer KIA", "https://www.kia.com/pe/main.html" },

                { "Bank of Nova Scotia", "https://www.scotiabank.com/ca/en" },
                { "Mercado Mexicano Canada", "https://www.mercadomexicano.ca/" },
                { "Scotiabank Perú", "https://www.scotiabank.com.pe/Personas/Default" },
                { "Scotiabank Peru", "https://www.scotiabank.com.pe/Personas/Default" },

                { "Scotiabank Perú – Perú", "https://www.scotiabank.com.pe/Personas/Default" },
                { "Soprole", "https://www.soprole.cl/"},
                { "Tecnyca", "https://tecnyca.net/"},

                { "Stratus Building Solutions", "https://www.stratusclean.com/" },
                { "Auphan Software corp", "https://www.auphansoftware.com/" },
                { "Global Hits", "https://globalhitss.com/" },
                { "IQ Outsourcing", "https://www.iqoutsourcing.com/" },
                { "Novoclick SAS", "https://www.novoclick.net/"},
                { "Tec Online SAS", "https://www.linkedin.com/in/leonardorive/"},
                { "Doka", "https://www.doka.com/ca/index"},
                { "Lean Solutions Group", "https://www.leangroup.com/"},
                { "Lean Solutions Group - Colombia", "https://www.leangroup.com/"},
                { "SYKES", "https://foundever.com/"},
                { "SYKES - Colombia", "https://foundever.com/"},
                { "Grupo Cobra", "https://www.grupocobra.com/en/" },
                { "Grupo Cobra - Colombia", "https://www.grupocobra.com/en/" }
            };

            XFont boldFont = new XFont("Verdana", 10, XFontStyleEx.Bold);
            XFont normalFont = new XFont("Verdana", 10, XFontStyleEx.Regular);
            double xPosition = margin;
            double currentYPosition = yPosition;
            string titleText = title;
            gfx.DrawString(titleText, boldFont, XBrushes.Black, new XPoint(xPosition, currentYPosition), XStringFormats.TopLeft);
            double titleWidth = gfx.MeasureString(titleText, boldFont).Width;
            xPosition += titleWidth;
            string separator = " | ";
            gfx.DrawString(separator, boldFont, XBrushes.Black, new XPoint(xPosition, currentYPosition), XStringFormats.TopLeft);
            double separatorWidth = gfx.MeasureString(separator, boldFont).Width;
            xPosition += separatorWidth;
            string companyText = company;
            gfx.DrawString(companyText, boldFont, XBrushes.Blue, new XPoint(xPosition, currentYPosition), XStringFormats.TopLeft);
            var siteCompany = string.Empty;
            try
            {
                siteCompany = urlCompanies[companyText] is null ? string.Empty : urlCompanies[companyText];
            }
            catch (Exception ex)
            {

                var i = ex;
            }
            var xrectCompany = new XRect(xPosition, currentYPosition, xPosition + 100, currentYPosition);
            var rectCompany = gfx.Transformer.WorldToDefaultPage(xrectCompany);
            var pdfrectCompany = new PdfRectangle(rectCompany);
            page.AddWebLink(pdfrectCompany, siteCompany);
            double companyWidth = gfx.MeasureString(companyText, boldFont).Width;
            xPosition += companyWidth;
            gfx.DrawString(separator, boldFont, XBrushes.Black, new XPoint(xPosition, currentYPosition), XStringFormats.TopLeft);
            xPosition += separatorWidth;
            string locationText = location;
            gfx.DrawString(locationText, boldFont, XBrushes.Black, new XPoint(xPosition, currentYPosition), XStringFormats.TopLeft);
            yPosition += 20;
            gfx.DrawString(dates, normalFont, XBrushes.Black, margin, yPosition);
            yPosition += 5;
            foreach (var bullet in bullets)
            {
                var buletLines = TextHelper.SplitText(bullet, 90);
                var bulletfirts = buletLines.First();
                var buletLinesLast = buletLines.Skip(1);
                gfx.DrawString($"• {bulletfirts}", normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, pageWidth - 2 * margin, pageWidth), XStringFormats.TopLeft);
                yPosition += 10;
                foreach (var bulletLine in buletLinesLast)
                {
                    gfx.DrawString($"{bulletLine}", normalFont, XBrushes.Black, new XRect(margin + 20, yPosition, pageWidth - 2 * margin, pageWidth), XStringFormats.TopLeft);
                    yPosition += 10;
                }
            }

            yPosition += 15;
            var techStackString = "Tech Stack: " + string.Join(" | ", techStack);
            var techStackList = TextHelper.SplitText(techStackString, 90);
            foreach (var tech in techStackList)
            {
                gfx.DrawString(tech, normalFont, XBrushes.Black, margin, yPosition);
                yPosition += 10;
            }
            //gfx.DrawString(techStackString, normalFont, XBrushes.Black, margin, yPosition);
            yPosition += 10;
        }
    }
}