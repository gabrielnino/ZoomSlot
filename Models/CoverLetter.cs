using System.Text.Json.Serialization;

namespace Models
{
    public class CoverLetter
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        [JsonPropertyName("Contact Information")]
        public ContactInformation ContactInformation { get; set; }
        [JsonPropertyName("Professional Summary")]
        public string ProfessionalSummary { get; set; }
        [JsonPropertyName("Bullet Points")]
        public List<string> BulletPoints { get; set; }
        [JsonPropertyName("Closing Paragraph")]
        public string ClosingParagraph { get; set; }
        public List<string> TechnicalSkills { get; set; }
        public List<string> SoftSkills { get; set; }
        public List<string> Languages { get; set; }
        public List<ProfessionalExperience> ProfessionalExperience { get; set; }
        [JsonPropertyName("Additional Qualifications")]
        public List<string> AdditionalQualifications { get; set; }
        public Education Education { get; set; }
    }
}
