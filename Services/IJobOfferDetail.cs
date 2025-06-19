namespace Services
{
    public interface IJobOfferDetail
    {
        Task<List<Models.JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers);
    }
}
