using Models;

namespace Services.Interfaces
{
    public interface IJobDetailsCoordinator
    {
        Task<List<@string>> DetailJobsAsync();
    }
}
