using Models;
namespace Services.Interfaces
{
    public interface IDetailProcessing
    {
        Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers, string searchText);
    }
}
