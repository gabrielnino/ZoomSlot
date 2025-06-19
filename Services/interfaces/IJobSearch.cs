namespace Services.interfaces
{
    public interface IJobSearch
    {
        Task<string> PerformSearchAsync();
    }
}
