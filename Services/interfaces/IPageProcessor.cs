namespace Services.Interfaces
{
    public interface IPageProcessor
    {
        Task<List<string>> ProcessAllPagesAsync();
    }
}
