namespace Services.Interfaces
{
    public interface IJobSearchCoordinator
    {
        Task<List<string>> SearchJobsAsync();
    }
}
