using System.Text.Json;
using Models;
using Services.Interfaces;
using Services.PDF;

namespace Services
{
    public class DocumentParse(IOpenAIClient openAIClient) : IDocumentParse
    {
        private readonly IOpenAIClient _openAIClient = openAIClient;
        private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

        public async Task<JobOffer> ParseJobOfferAsync(string jobOfferDescription)
        {
            var prompt = PrompHelpers.GetParseJobOfferPrompt(jobOfferDescription);
            var jobOfferSummary = await _openAIClient.GetChatCompletionAsync(prompt);
            jobOfferSummary = StringHelpers.ExtractJsonContent(jobOfferSummary);
            JobOffer? jobOffer = JsonSerializer.Deserialize<JobOffer>(jobOfferSummary, options);
            return jobOffer;
        }

        public async Task<Resume> ParseResumeAsync(string resumeString)
        {
            var prompt = PrompHelpers.GetParseResumePrompt(resumeString);
            var resumeeJson = await _openAIClient.GetChatCompletionAsync(prompt);
            resumeeJson = StringHelpers.ExtractJsonContent(resumeeJson);
            Resume? resume = JsonSerializer.Deserialize<Resume>(resumeeJson, options);
            return resume;
        }
    }
}
