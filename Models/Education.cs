using System.Text.Json.Serialization;

namespace Models
{
    public class Education
    {
        public string Institution { get; set; }
        public string Location { get; set; }
        public string Degree { get; set; }

        [JsonPropertyName("Graduation Date")]
        public string GraduationDate { get; set; }
    }
}
