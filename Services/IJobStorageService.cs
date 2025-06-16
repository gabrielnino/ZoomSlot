using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public interface IJobStorageService
    {
        /// <summary>
        /// Loads all stored job offers
        /// </summary>
        Task<IEnumerable<JobOffer>> LoadJobsAsync();

        /// <summary>
        /// Saves a collection of job offers to storage
        /// </summary>
        Task SaveJobsAsync(IEnumerable<JobOffer> jobs);

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
