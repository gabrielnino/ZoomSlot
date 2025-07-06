using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class QualifiedService : IQualifiedService
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly IJobStorageService _jobStorageService;
        private readonly ILogger<QualifiedService> _logger;
        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

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
                var evaluationPrompt = PrompHelpers.GetQualifiedPrompt(resume, offer.RawJobDescription);
                _logger.LogDebug("Generated evaluation prompt for job offer: {Prompt}", evaluationPrompt);
                var score = await _openAIClient.GetChatCompletionAsync(evaluationPrompt);
                var scoreJson = StringHelpers.ExtractJsonContent(score);
                _logger.LogDebug("Extracted JSON evaluation result for resume.");
                ResumeMatch? resumeMatch = JsonSerializer.Deserialize<ResumeMatch>(scoreJson, _options);
                offer.AiFitScore = resumeMatch?.Score ?? 0;
            }
            var timeStan = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(offersFilePath);
            var basePath = Path.GetDirectoryName(offersFilePath);
            var offersFilePathFinal = Path.Combine(basePath ?? string.Empty, $"{fileName}_qualified_{timeStan}.json");
            await _jobStorageService.SaveJobOfferAsync(offersFilePathFinal, offers);
        }
    }
}
