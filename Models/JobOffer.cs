namespace Models
{
    using System.Text.Json.Serialization;

    public class JobOffer
    {

        [JsonPropertyName("Company Name")]
        public string CompanyName { get; set; }

        [JsonPropertyName("Job Offer Title")]
        public string JobOfferTitle { get; set; }

        [JsonPropertyName("Job Offer Summarize")]
        public string JobOfferSummarize { get; set; }

        [JsonPropertyName("Email Contact")]
        public string EmailContact { get; set; }
        public string ContactHiringSection { get; set; }

        [JsonPropertyName("Key Skills Required")]
        public List<string> KeySkillsRequired { get; set; }

        [JsonPropertyName("Essential Qualifications")]
        public List<string> EssentialQualifications { get; set; }

        [JsonPropertyName("Essential Technical Skill Qualifications")]
        public List<string> EssentialTechnicalSkillQualifications { get; set; }
        [JsonPropertyName("Other Technical Skill Qualifications")]
        public List<string> OtherTechnicalSkillQualifications { get; set; }

        [JsonPropertyName("Salary or Budget Offered")]
        public string SalaryOrBudgetOffered { get; set; }

        public string Description { get; set; }
        public string Url { get; set; }
        public IEnumerable<string> RawJobDescription { get; set; }
    }
}
