namespace Services
{
    public interface IPageProcessor
    {
        Task<List<string>> ProcessAllPagesAsync();
    }
}
