using Models;

namespace Services.Interfaces
{
    public interface IPageTrackingService
    {
        Task<PageTrackingState> LoadPageStateAsync(string searchId);
        Task SavePageStateAsync(string searchId, PageTrackingState state);
        Task ClearPageStateAsync(string searchId);
    }
}
