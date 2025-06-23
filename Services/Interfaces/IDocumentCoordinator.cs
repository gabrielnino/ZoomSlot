namespace Services.Interfaces
{
    public interface IDocumentCoordinator
    {
        Task GenerateDocumentAsync(string inputResume, string urlJobBoard);
    }
}
