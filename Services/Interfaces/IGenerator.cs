using Models;

namespace Services.Interfaces
{
    public interface IGenerator
    {
        Task<Resume> CreateResume(JobOffer jobOffer, Resume resume);
        Task<CoverLetter> CreateCoverLetter(JobOffer jobOffer, Resume resume);
    }
}
