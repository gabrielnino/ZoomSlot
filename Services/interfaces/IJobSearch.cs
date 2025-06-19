namespace Services.Interfaces
{
    public interface IJobSearch
    {
        Task<string> PerformSearchAsync();
    }
}
