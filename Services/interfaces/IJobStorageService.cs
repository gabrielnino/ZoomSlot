using Models;

namespace Services.Interfaces
{
    public interface IJobStorageService
    {
        Task<IEnumerable<JobOfferDetail>> LoadJobsAsync();
        Task SaveJobsAsync(IEnumerable<JobOfferDetail> jobs);
        Task<int> GetJobCountAsync();
        Task ClearStorageAsync();
    }
}
