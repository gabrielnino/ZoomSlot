namespace Services
{
    public interface IDetailProcessing
    {
        Task<List<Models.JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers);
    }
}
