namespace Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class JobOffer
    {
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public string JobOfferTitle { get; set; }
        public string JobOfferSummarize { get; set; }
        public string EmailContact { get; set; }
        public string ContactHiringSection { get; set; }
        public List<Skill> KeySkillsRequired { get; set; }
        public List<Skill> EssentialQualifications { get; set; }
        public List<Skill> EssentialTechnicalSkillQualifications { get; set; }
        public List<Skill> OtherTechnicalSkillQualifications { get; set; }
        public string SalaryOrBudgetOffered { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        private int? _aiFitScore;

        [Range(0, 100, ErrorMessage = "Fit score must be between 0 and 100.")]
        public int? AiFitScore
        {
            get => _aiFitScore;
            set
            {
                if (value is < 0 or > 100)
                    throw new ArgumentOutOfRangeException(nameof(AiFitScore), "Value must be between 0 and 100 if not null.");
                _aiFitScore = value;
            }
        }
        public IEnumerable<string> RawJobDescription { get; set; }
    }
}
