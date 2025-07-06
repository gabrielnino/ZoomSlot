using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services
{
    public class QualifiedService : IQualifiedService
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly IJobStorageService _jobStorageService;
        private readonly ILogger<QualifiedService> _logger;

        public QualifiedService(IJobStorageService jobStorageService, IOpenAIClient openAIClient, ILogger<QualifiedService> logger)
        {
            _openAIClient = openAIClient;
            _jobStorageService = jobStorageService;
            _logger = logger;
        }
        public async Task QualifiedAsync(string offersFilePath, string resumeFilePath)
        {
            var offers = await _jobStorageService.LoadJobsAsync(offersFilePath);
            var resume = await _jobStorageService.LoadFileAsync(resumeFilePath);
            foreach (var offer in offers.ToList())
            {
                var prompt = PrompHelpers.GetQualifiedPrompt(resume, offer.RawJobDescription);
                _logger.LogDebug("Generated job offer prompt: {Prompt}", prompt);
                var jobOfferSummary = await _openAIClient.GetChatCompletionAsync(prompt);
            }
        }
    }
}
