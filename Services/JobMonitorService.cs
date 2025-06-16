using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Models;

namespace Services
{
    public class JobMonitorService : IDisposable
    {
        private readonly Subject<JobOffer> _jobOfferSubject = new();
        private readonly ILogger<JobMonitorService> _logger;

        public JobMonitorService(ILogger<JobMonitorService> logger)
        {
            _logger = logger;

            // Example subscription
            _jobOfferSubject
                .Buffer(TimeSpan.FromSeconds(5))  // Batch every 5 seconds
                .Subscribe(batch =>
                {
                    if (batch.Count > 0)
                    {
                        _logger.LogInformation($"Processing batch of {batch.Count} jobs");
                    }
                });
        }

        public void PublishJob(JobOffer offer)
        {
            _jobOfferSubject.OnNext(offer);
        }

        public void Dispose()
        {
            _jobOfferSubject?.OnCompleted();
            _jobOfferSubject?.Dispose();
        }
    }
}
