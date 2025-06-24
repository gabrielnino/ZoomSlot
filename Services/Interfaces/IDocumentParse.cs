using Models;

namespace Services.Interfaces
{
    public interface IDocumentParse
    {
        Task<JobOffer> ParseJobOfferAsync(string jobOfferDescription);
        Task<Resume> ParseResumeAsync(string resumeString);
    }
}
