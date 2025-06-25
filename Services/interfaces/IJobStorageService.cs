using Models;

namespace Services.Interfaces
{
    public interface IJobStorageService
    {
        Task<IEnumerable<string>> LoadJobsAsync();
        Task<List<string>>? LoadOffersAsync(string offersFilePath);
        Task SaveOffersAsync(List<string> offers, string offersFilePath);
        Task<IEnumerable<JobOfferDetail>> LoadJobsDetailAsync();
        Task SaveJobOfferDetailAsync(IEnumerable<JobOfferDetail> jobs);
        Task SaveOffersDetailAsync(List<JobOfferDetail> offersDetail, string offersDetailFilePath);
        Task SaveJobsAsync(IEnumerable<string> jobs);
        Task SavePendingOffersAsync(string offersFilePath, List<string> offersPending);
        Task<int> GetJobCountAsync();
        Task ClearStorageAsync();
    }
}
