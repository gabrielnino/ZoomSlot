namespace Models
{
    public class JobOffer
    {
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public string JobOfferTitle { get; set; }
        public string JobOfferSummarize { get; set; }
        public string EmailContact { get; set; }
        public string ContactHiringSection { get; set; }
        public List<Skill> KeySkillsRequired { get; set; }
        public Dictionary<string, List<Skill>> Skills { get; set; }
        public List<Skill> EssentialQualifications { get; set; }
        public List<Skill> EssentialTechnicalSkillQualifications { get; set; }
        public List<Skill> OtherTechnicalSkillQualifications { get; set; }
        public string SalaryOrBudgetOffered { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public IEnumerable<string> RawJobDescription { get; set; }
    }
}
