using System.Text.Json;
using Microsoft.Extensions.Options;
using Models;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class Generator : IGenerator
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        public Generator(IOpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        public async Task<Resume> CreateResume(JobOffer jobOffer, Resume resume)
        {
            var stringJobOffer = JsonConvert.SerializeObject(jobOffer, Formatting.Indented);
            var stringResume = JsonConvert.SerializeObject(resume, Formatting.Indented);
            var prompt = PrompHelpers.GenerateResumeJsonPrompt(stringJobOffer, stringResume);
            var stringGenerateResume = await _openAIClient.GetChatCompletionAsync(prompt);
            stringGenerateResume = StringHelpers.ExtractJsonContent(stringGenerateResume);
            var generateResume = JsonConvert.DeserializeObject<Resume>(stringGenerateResume);
            if (generateResume == null)
            {
                throw new Exception("Failed to deserialize the resume from OpenAI response.");
            }
            return generateResume;
        }

        public async Task<CoverLetter> CreateCoverLetter(JobOffer jobOffer, Resume resume)
        {
            var stringJobOffer = JsonConvert.SerializeObject(jobOffer, Formatting.Indented);
            var stringResume = JsonConvert.SerializeObject(resume, Formatting.Indented);
            var prompt = PrompHelpers.GenerateCoverLetterPrompt(stringJobOffer, stringResume);
            var stringCoverLetter = await _openAIClient.GetChatCompletionAsync(prompt);
            stringCoverLetter = StringHelpers.ExtractJsonContent(stringCoverLetter);
            var coverLetter = JsonConvert.DeserializeObject<CoverLetter>(stringCoverLetter);
            if(coverLetter == null)
            {
                throw new Exception("Failed to deserialize the cover letter from OpenAI response.");
            }
            return coverLetter;
        }
    }
}
