using Models;

namespace Services.Interfaces
{
    public interface IQualifiedService
    {
        Task QualifiedAsync(string offersFilePath,string resumeFilePath);
    }
}
