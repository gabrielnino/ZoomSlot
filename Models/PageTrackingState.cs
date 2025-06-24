namespace Models
{
    public class PageTrackingState
    {
        public int LastProcessedPage { get; set; } = 0;
        public List<string> CollectedOffers { get; set; } = new();
        public bool IsComplete { get; set; } = false;
    }
}
