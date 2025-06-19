namespace Services.Interfaces
{
    public interface ICaptureSnapshot
    {
        Task<string> CaptureArtifactsAsync(string executionFolder, string stage);
    }
}
