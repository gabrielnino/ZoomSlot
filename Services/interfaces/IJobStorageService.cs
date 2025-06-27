using Models;

namespace Services.Interfaces
{
    public interface IJobStorageService
    {
        Task<IEnumerable<string>>? LoadOffersAsync(string offersFilePath);
        Task<IEnumerable<JobOfferDetail>> LoadJobsDetailAsync(string offersFilePath);
        Task<IEnumerable<JobOffer>> LoadJobsAsync(string offersFilePath);
        Task SaveOffersAsync(string offersFilePath, IEnumerable<string> offersPending);
        Task SaveJobOfferDetailAsync(string offersDetailFilePath, IEnumerable<JobOfferDetail> offersDetail);
        Task SaveJobOfferAsync(string offersDetailFilePath, IEnumerable<JobOffer> offers);
        string StorageFile { get; }
    }
}
