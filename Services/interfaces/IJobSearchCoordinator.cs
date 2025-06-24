using Models;

namespace Services.Interfaces
{
    public interface IJobSearchCoordinator
    {
        Task<List<string>> SearchJobsAsync();
    }
}
