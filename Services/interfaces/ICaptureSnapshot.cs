namespace Services.interfaces
{
    public interface ICaptureSnapshot
    {
        Task<string> CaptureArtifacts(string executionFolder, string stage);
    }
}
