using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public interface IJobMonitorService : IDisposable
    {
        /// <summary>
        /// Publishes a new job offer to the monitoring stream
        /// </summary>
        void PublishNewOffer(JobOffer offer);

        /// <summary>
        /// Observable stream of job offers
        /// </summary>
        IObservable<JobOffer> JobOfferStream { get; }

        /// <summary>
        /// Gets the current count of processed offers
        /// </summary>
        int ProcessedOffersCount { get; }

        /// <summary>
        /// Gets all observed offers (for snapshot purposes)
        /// </summary>
        IEnumerable<JobOffer> ObservedOffers { get; }

        /// <summary>
        /// Event triggered when batch processing completes
        /// </summary>
        event EventHandler<JobBatchEventArgs> BatchProcessed;
    }

    public class JobBatchEventArgs : EventArgs
    {
        public int BatchSize { get; }
        public DateTimeOffset ProcessingTime { get; }

        public JobBatchEventArgs(int batchSize)
        {
            BatchSize = batchSize;
            ProcessingTime = DateTimeOffset.UtcNow;
        }
    }
}
