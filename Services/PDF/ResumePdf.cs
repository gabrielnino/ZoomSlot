namespace Services.PDF
{
    using Models;
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;
    using Services.PDF.Addional;

    public static class ResumePdf
    {
        public static void Generate(string outputPath, ResumeRequest resumeRequest)
        {
            const string familyName = "Verdana";
            var document = new PdfDocument();
            PdfPage page, page2;
            XGraphics gfx, gfx2;
            double margin, interline, yPosition, yPosition2;
            XFont normalFont, subTitleFont;
            string qualifications = string.Join(" | ", resumeRequest.JobOffer.EssentialQualifications);
            PdfHeaderBuilder.GetHeader(resumeRequest.JobOffer, resumeRequest.Resume, familyName, document, out page, out gfx, out margin, out interline, out yPosition, out normalFont, out subTitleFont, resumeRequest.UrlJobBoard);
            double interlineParagraph = 10;
            var lines = TextHelper.SplitText(resumeRequest.Resume.ProfessionalSummary, 100);
            ProfessionalSummary.AddProfessionalSummary(gfx, normalFont, lines, margin, ref yPosition, page, interlineParagraph);
            var sectionTitleFont = new XFont(familyName, 14, XFontStyleEx.Bold);
            gfx.DrawString("Technical Skills", sectionTitleFont, XBrushes.Black, margin, yPosition);
            yPosition += 5;
            var limitQualifications = 10;
            var essentialQualifications = resumeRequest.JobOffer.EssentialTechnicalSkillQualifications.Take(limitQualifications).ToList();
            var qualificationsCount = essentialQualifications.Count();
            if (essentialQualifications.Any() && qualificationsCount<limitQualifications)
            {
                var essentialOtherQualifications = resumeRequest.JobOffer.OtherTechnicalSkillQualifications.Take(limitQualifications-qualificationsCount);
                essentialQualifications.AddRange(essentialOtherQualifications);
            }

            string[] value = [.. essentialQualifications];
            var qualificationsResume = string.Join(" | ", value);
            var languages = string.Join(", ", qualificationsResume) + " - Languages: " + string.Join(" , ", resumeRequest.Resume.Languages);
            var languagesList = TextHelper.SplitText(languages, 87);
            TechnicalSkills.AddTechnicalSkills(gfx, normalFont, languagesList, margin, ref yPosition, page, interlineParagraph);
            yPosition += 15;
            var boldFont = new XFont(familyName, 10, XFontStyleEx.Bold);
            gfx.DrawString("Professional Experience", sectionTitleFont, XBrushes.Black, margin, yPosition);
            yPosition += 10;
            var experieces = resumeRequest.Resume.ProfessionalExperiences;
            var page1Experieces = experieces.Take(2);
            foreach (var experience in page1Experieces)
            {
                string titleText = experience.Role.Length>37 ? experience.Role[..37] : experience.Role;
                Experience.AddExperience(gfx, titleText, experience.Company, experience.Location, experience.Duration, experience.Responsibilities.ToArray(), margin, ref yPosition, page.Width, experience.TechStack.ToArray(), ref page);
            }

            PdfHeaderBuilder.AddPage(document, out page2, out gfx2);
            var page2Experieces = experieces.Skip(2);
            yPosition2 = margin;
            foreach (var experience in page2Experieces)
            {
                Experience.AddExperience(gfx2, experience.Role, experience.Company, experience.Location, experience.Duration, experience.Responsibilities.ToArray(), margin, ref yPosition2, page2.Width, experience.TechStack.ToArray(), ref page2);
            }
            yPosition2 += 10;
            gfx2.DrawString("ADDITIONAL QUALIFICATIONS", sectionTitleFont, XBrushes.Black, margin, yPosition2);
            AddionalQualitions.AddAddionalQualitions(gfx2, resumeRequest.Resume.AdditionalQualifications.ToArray(), margin, ref yPosition2, page2.Width);
            yPosition2 += 10;
            gfx2.DrawString("EDUCATION", sectionTitleFont, XBrushes.Black, margin, yPosition2);
            yPosition2 += 15;
            gfx2.DrawString(resumeRequest.Resume.Education.Institution + " - " + resumeRequest.Resume.Education.Location, normalFont, XBrushes.Black, margin, yPosition2);
            yPosition2 += 10;
            gfx2.DrawString(resumeRequest.Resume.Education.Degree + ", " + resumeRequest.Resume.Education.GraduationDate, normalFont, XBrushes.Black, margin, yPosition2);
            document.Save(outputPath);
        }
    }
}