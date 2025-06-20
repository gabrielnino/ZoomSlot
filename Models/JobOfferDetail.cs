using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class JobOfferDetail
    {
        [Key]
        public required string ID { get; set; }
        public required string JobOfferTitle { get; set; }
        public required string CompanyName { get; set; }
        public required string ContactHiringSection { get; set; }
        public required string Description { get; set; }
        public required string SalaryOrBudgetOffered { get; set; }
        public required string Link { get; set; }
        public required string Applicants { get; set; }
        public required string SearchText { get; set; }
        
    }
}
