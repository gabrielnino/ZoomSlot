namespace Services.Interfaces
{
    public interface IJobStorageService
    {
        /// <summary>
        /// Loads all stored job offers
        /// </summary>
        Task<IEnumerable<string>> LoadJobsAsync();

        /// <summary>
        /// Saves a collection of job offers to storage
        /// </summary>
        Task SaveJobsAsync(IEnumerable<string> jobs);

        /// <summary>
        /// Optional: Gets the count of stored jobs
        /// </summary>
        Task<int> GetJobCountAsync();

        /// <summary>
        /// Optional: Clears all stored job data
        /// </summary>
        Task ClearStorageAsync();
    }
}
