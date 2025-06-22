using System.Text.Json;
using Models;
using Services.Interfaces;

namespace Services
{
    public class DocumentMapper : IDocumentMapper
    {
        private readonly IOpenAIClient _openAIClient;
        public DocumentMapper(IOpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }
        public async Task<JobOffer> GetJobOffer(string jobOfferDescription)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var prompt = PrompHelpers.GenerateJobOfferJsonPrompt(jobOfferDescription);
            var jobOfferSummary = await _openAIClient.GetChatCompletionAsync(prompt);
            jobOfferSummary = StringHelpers.ExtractJsonContent(jobOfferSummary);
            JobOffer? jobOffer = JsonSerializer.Deserialize<JobOffer>(jobOfferSummary, options);
            return jobOffer;
        }

        public async Task<Resume> GetResumee(string resumeString)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var prompt = PrompHelpers.GenerateResumeeJsonPrompt(resumeString);
            var resumeeJson = await _openAIClient.GetChatCompletionAsync(prompt);
            resumeeJson = StringHelpers.ExtractJsonContent(resumeeJson);
            Resume? resume = JsonSerializer.Deserialize<Resume>(resumeeJson, options);
            return resume;
        }
    }
}
