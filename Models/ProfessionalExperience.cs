using System.Text.Json.Serialization;

namespace Models
{
    public class ProfessionalExperience
    {
        public string Role { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public string Duration { get; set; }
        public List<string> Responsibilities { get; set; }

        [JsonPropertyName("Tech Stack")]
        public List<string> TechStack { get; set; }
    }
}
