using System.Text.Json;
using Models;
using Services.Interfaces;

namespace Services
{
    public class DocumentMapper(IOpenAIClient openAIClient) : IDocumentMapper
    {
        private readonly IOpenAIClient _openAIClient = openAIClient;
        private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public async Task<JobOffer> GetJobOffer(string jobOfferDescription)
        {
            var prompt = PrompHelpers.GenerateJobOfferJsonPrompt(jobOfferDescription);
            var jobOfferSummary = await _openAIClient.GetChatCompletionAsync(prompt);
            jobOfferSummary = StringHelpers.ExtractJsonContent(jobOfferSummary);
            JobOffer? jobOffer = JsonSerializer.Deserialize<JobOffer>(jobOfferSummary, options);
            return jobOffer;
        }

        public async Task<Resume> GetResumee(string resumeString)
        {
            var prompt = PrompHelpers.GenerateResumeJsonPrompt(resumeString);
            var resumeeJson = await _openAIClient.GetChatCompletionAsync(prompt);
            resumeeJson = StringHelpers.ExtractJsonContent(resumeeJson);
            Resume? resume = JsonSerializer.Deserialize<Resume>(resumeeJson, options);
            return resume;
        }
    }
}
