using Models;

namespace Services.Interfaces
{
    public interface IJobDetailsCoordinator
    {
        Task<List<JobOfferDetail>> DetailJobsAsync(List<string> job, string searchText);
    }
}
