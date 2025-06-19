using Models;

namespace Services
{
    public interface IDetailProcessing
    {
        Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText);
    }
}
