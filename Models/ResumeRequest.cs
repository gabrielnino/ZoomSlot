namespace Models
{
    public class ResumeRequest
    {
        public string UrlJobBoard { get; set; }
        public JobOffer JobOffer { get; set; }
        public Resume Resume { get; set; }

    }
}
