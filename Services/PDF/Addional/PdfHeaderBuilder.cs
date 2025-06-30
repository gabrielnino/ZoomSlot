namespace Services.PDF.Addional
{
    using System.Reflection;
    using Models;
    using PdfSharp.Drawing;
    using PdfSharp.Fonts;
    using PdfSharp.Pdf;

    internal static class PdfHeaderBuilder
    {
        public static void GetHeader(JobOffer jobOffer, Resume resume, string familyName, PdfDocument document, out PdfPage page, out XGraphics gfx, out double margin, out double interline, out double yPosition, out XFont normalFont, out XFont subTitleFont, string siteUri)
        {
            var limitQualifications = 10;
            var essentialQualifications = jobOffer.EssentialTechnicalSkillQualifications.Take(limitQualifications).ToList();
            var qualificationsCount = essentialQualifications.Count();
            if (essentialQualifications.Any() && qualificationsCount < limitQualifications)
            {
                var essentialOtherQualifications = jobOffer.OtherTechnicalSkillQualifications.Take(limitQualifications - qualificationsCount);
                essentialQualifications.AddRange(essentialOtherQualifications);
            }

            //string[] value = essentialQualifications.ToArray();
            //var qualifications = string.Join(" | ", value);
            document.Info.Title = resume.Name;
            AddPage(document, out page, out gfx);
            margin = 50;
            interline = 20;
            yPosition = margin;
            XFont titleFont;
            VerdanaFontResolver verdanaFontResolver = new VerdanaFontResolver();

            GlobalFontSettings.FontResolver = verdanaFontResolver;

            titleFont = new XFont(familyName, 18, XFontStyleEx.Bold);


            gfx.DrawString(resume.Name, titleFont, XBrushes.Black, margin, yPosition);
            yPosition += interline;

            var emojiFont = new XFont(familyName, 12, XFontStyleEx.Regular);
            var xrect = new XRect(margin + 270, yPosition - 15, 390, 20);
            var rect = gfx.Transformer.WorldToDefaultPage(xrect);
            var pdfrect = new PdfRectangle(rect);
            page.AddWebLink(pdfrect, siteUri);

            normalFont = new XFont(familyName, 10, XFontStyleEx.Regular);


            {

                // Load the images from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                XImage linkedinImage = LoadEmbeddedImage(assembly, "Services.PDF.Addional.Images.linkedin-icon.png");
                XImage emailImage = LoadEmbeddedImage(assembly, "Services.PDF.Addional.Images.email-icon.png");
                XImage phoneImage = LoadEmbeddedImage(assembly, "Services.PDF.Addional.Images.phone-icon.png");

                double imageWidth = 12;
                double imageHeight = 12;

                // LinkedIn Image
                gfx.DrawImage(linkedinImage, margin + 400, yPosition - imageHeight + 2, imageWidth, imageHeight);
                gfx.DrawString("LinkedIn", normalFont, XBrushes.Blue, margin + 340, yPosition);

                // Email Image
                var siteUriEmail = $"mailto:{resume.ContactInfo.Email}";
                var xrectEmail = new XRect(margin + 120, yPosition - 15, 140, 20);
                var rectEmail = gfx.Transformer.WorldToDefaultPage(xrectEmail);
                var pdfrectEmail = new PdfRectangle(rectEmail);
                page.AddWebLink(pdfrectEmail, siteUriEmail);
                gfx.DrawImage(emailImage, margin + 150, yPosition - imageHeight + 2, imageWidth, imageHeight);
                gfx.DrawString(resume.ContactInfo.Email, normalFont, XBrushes.Blue, margin + 170, yPosition);

                // Phone Image
                gfx.DrawImage(phoneImage, margin, yPosition - imageHeight + 2, imageWidth, imageHeight);
                gfx.DrawString($"{resume.ContactInfo.Phone}", emojiFont, XBrushes.Black, margin + 20, yPosition);

                yPosition += interline;
                subTitleFont = new XFont(familyName, 12, XFontStyleEx.Regular);

                var titleJobOffer = jobOffer.JobOfferTitle;
                //var skills = qualifications.Count() > 0 ? " | " + qualifications : string.Empty;
                //var qualificationsList = TextHelper.SplitText(jobOffer.JobOfferTitle + skills, 72);
                //foreach (var qualification in qualificationsList)
                //{
                //    gfx.DrawString(qualification, subTitleFont, XBrushes.Black, margin, yPosition);
                //    yPosition += interline;
                //}
            }
        }

        public static void AddPage(PdfDocument document, out PdfPage page, out XGraphics gfx)
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
        }

        private static XImage LoadEmbeddedImage(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                }
                return XImage.FromStream(stream);
            }
        }
    }
}