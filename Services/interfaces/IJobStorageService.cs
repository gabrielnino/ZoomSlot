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
        Task SaveJobsAsync(IEnumerable<string> jobs);
        Task<int> GetJobCountAsync();
        Task ClearStorageAsync();
    }
}
