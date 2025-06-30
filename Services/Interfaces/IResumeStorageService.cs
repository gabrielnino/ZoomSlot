using Models;

namespace Services.Interfaces
{
    public interface IResumeStorageService
    {
        Task SaveResumeAsync(string resumeFilePath, Resume resume);
        Task<string> LoadResumeAsync(string offersFilePath);
        string StorageFile { get; }
    }
}
