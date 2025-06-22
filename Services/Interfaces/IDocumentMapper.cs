
using System.Text.Json;
using Models;

namespace Services.Interfaces
{
    public interface IDocumentMapper
    {
        Task<JobOffer> GetJobOffer(string jobOfferDescription);
        Task<Resume> GetResumee(string resumeString);
    }
}
