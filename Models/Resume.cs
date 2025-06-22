using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Models
{
    public class Resume
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }

        [JsonPropertyName("Contact Information")]
        public ContactInformation ContactInfo { get; set; }

        [JsonPropertyName("Professional Summary")]
        public string ProfessionalSummary { get; set; }

        [JsonPropertyName("Technical Skills")]
        public List<string> TechnicalSkills { get; set; }

        [JsonPropertyName("Soft Skills")]
        public List<string> SoftSkills { get; set; }
        public List<string> Languages { get; set; }

        [JsonPropertyName("Professional Experience")]
        public List<ProfessionalExperience> ProfessionalExperiences { get; set; }

        [JsonPropertyName("Additional Qualifications")]
        public List<string> AdditionalQualifications { get; set; }
        public Education Education { get; set; }
    }
}
