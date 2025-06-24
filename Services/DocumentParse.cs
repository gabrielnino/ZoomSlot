using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Services.Interfaces;

namespace Services
{
    public class DocumentParse(IOpenAIClient openAIClient, ILogger<DocumentParse> logger) : IDocumentParse
    {
        private readonly IOpenAIClient _openAIClient = openAIClient;
        private readonly ILogger<DocumentParse> _logger = logger;
        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        public async Task<JobOffer> ParseJobOfferAsync(string jobOfferDescription)
        {
            _logger.LogInformation("📝 Parsing job offer description...");
            var prompt = PrompHelpers.GetParseJobOfferPrompt(jobOfferDescription);
            _logger.LogDebug("Generated job offer prompt: {Prompt}", prompt);
            var jobOfferSummary = await _openAIClient.GetChatCompletionAsync(prompt);
            _logger.LogDebug("Received job offer summary response.");
            jobOfferSummary = StringHelpers.ExtractJsonContent(jobOfferSummary);
            _logger.LogDebug("Extracted JSON content for job offer.");
            JobOffer? jobOffer = JsonSerializer.Deserialize<JobOffer>(jobOfferSummary, _options);
            if (jobOffer == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize job offer JSON.");
                throw new InvalidOperationException("Unable to parse job offer JSON.");
            }
            _logger.LogInformation("✅ Job offer parsed successfully.");
            return jobOffer;
        }

        public async Task<Resume> ParseResumeAsync(string resumeString)
        {
            _logger.LogInformation("📝 Parsing resume string...");
            var prompt = PrompHelpers.GetParseResumePrompt(resumeString);
            _logger.LogDebug("Generated resume prompt: {Prompt}", prompt);
            var resumeJson = await _openAIClient.GetChatCompletionAsync(prompt);
            _logger.LogDebug("Received resume response.");
            resumeJson = StringHelpers.ExtractJsonContent(resumeJson);
            _logger.LogDebug("Extracted JSON content for resume.");
            Resume? resume = JsonSerializer.Deserialize<Resume>(resumeJson, _options);
            if (resume == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize resume JSON.");
                throw new InvalidOperationException("Unable to parse resume JSON.");
            }
            _logger.LogInformation("✅ Resume parsed successfully.");
            return resume;
        }
    }
}
