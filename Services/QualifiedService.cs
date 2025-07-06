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
            _logger.LogInformation("🔍 Loading job offers from: {OffersFile}", offersFilePath);
            var offers = await _jobStorageService.LoadJobsAsync(offersFilePath);
            _logger.LogInformation("📄 Loading resume from: {ResumeFile}", resumeFilePath);
            var resume = await _jobStorageService.LoadFileAsync(resumeFilePath);
            var offerList = offers.ToList();
            _logger.LogInformation("⚙️ Starting qualification process for {Count} job offers.", offerList.Count);

            for (int i = 0; i < offerList.Count; i++)
            {
                var offer = offerList[i];
                _logger.LogInformation("🧠 [{Current}/{Total}] Evaluating job: '{Title}' at '{Company}'",
                    i + 1, offerList.Count, offer.JobOfferTitle, offer.CompanyName);
                var evaluationPrompt = PrompHelpers.GetQualifiedPrompt(resume, offer.RawJobDescription);
                _logger.LogDebug("📤 Evaluation prompt generated:\n{Prompt}", evaluationPrompt);
                var score = await _openAIClient.GetChatCompletionAsync(evaluationPrompt);
                var scoreJson = StringHelpers.ExtractJsonContent(score);
                _logger.LogDebug("📥 Extracted JSON result:\n{Json}", scoreJson);
                ResumeMatch? resumeMatch = JsonSerializer.Deserialize<ResumeMatch>(scoreJson, _options);
                offer.AiFitScore = resumeMatch?.Score ?? 0;
                _logger.LogInformation("✅ AI Fit Score assigned: {Score}", offer.AiFitScore);
            }

            var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(offersFilePath);
            var basePath = Path.GetDirectoryName(offersFilePath);
            var offersFilePathFinal = Path.Combine(basePath ?? string.Empty, $"{fileName}_qualified_{timeStamp}.json");

            _logger.LogInformation("💾 Saving qualified job offers to: {OutputFile}", offersFilePathFinal);
            await _jobStorageService.SaveJobOfferAsync(offersFilePathFinal, offerList);
            _logger.LogInformation("🎯 Resume qualification completed successfully. {Count} offers processed.", offerList.Count);
        }

    }
}
