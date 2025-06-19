namespace Services
{
    public interface IJobSearch
    {
        Task<string> PerformSearchAsync();
    }
}
