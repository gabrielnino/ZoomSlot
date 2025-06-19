using Models;
namespace Services.interfaces
{
    public interface IDetailProcessing
    {
        Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText);
    }
}
