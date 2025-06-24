namespace Models
{
    public class CoverLetterRequest
    {
        public string UrlJobBoard { get; set; }
        public JobOffer JobOffer { get; set; }
        public Resume Resume { get; set; }
        public CoverLetter CoverLetter { get; set; }
    }
}
