using Models;

namespace Services.Interfaces
{
    public interface IJobDocumentCoordinator
    {
        Task<IEnumerable<JobOffer>> GenerateJobsDocumentAsync();
    }
}
