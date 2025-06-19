namespace Services.interfaces
{
    public interface IPageProcessor
    {
        Task<List<string>> ProcessAllPagesAsync();
    }
}
