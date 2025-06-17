namespace Services
{
    public interface ILinkedInService
    {
        Task SearchJobsAsync();
        Task<IEnumerable<string>> GetCurrentPageOffersAsync();
        Task<bool> NavigateToNextPageAsync();
    }
}
