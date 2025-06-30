using Models;

namespace Services.Interfaces
{
    public interface IResumeDocumentCoordinator
    {
        Task<Resume> GenerateResumeDocumentAsync(string resumeText);
    }
}
