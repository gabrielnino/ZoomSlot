using Models;

namespace Services.Interfaces
{
    public interface IJobStorageService
    {
        Task<IEnumerable<string>>? LoadOffersAsync(string offersFilePath);
        Task<IEnumerable<JobOfferDetail>> LoadJobsDetailAsync(string offersFilePath);
        Task SaveOffersAsync(string offersFilePath, IEnumerable<string> offersPending);
        Task SaveJobOfferDetailAsync(string offersDetailFilePath, IEnumerable<JobOfferDetail> offersDetail);
        string StorageFile { get; }
    }
}
