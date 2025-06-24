using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class Generator : IGenerator
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<Generator> _logger;
        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        public Generator(IOpenAIClient openAIClient, ILogger<Generator> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<Resume> CreateResume(JobOffer jobOffer, Resume resume)
        {
            _logger.LogInformation("🚀 Starting resume generation...");

            var stringJobOffer = JsonConvert.SerializeObject(jobOffer, Formatting.Indented);
            var stringResume = JsonConvert.SerializeObject(resume, Formatting.Indented);

            var prompt = PrompHelpers.GenerateResumeJsonPrompt(stringJobOffer, stringResume);
            _logger.LogDebug("Generated resume prompt: {Prompt}", prompt);

            var stringGenerateResume = await _openAIClient.GetChatCompletionAsync(prompt);
            _logger.LogDebug("Received resume JSON response.");

            stringGenerateResume = StringHelpers.ExtractJsonContent(stringGenerateResume);
            _logger.LogDebug("Extracted JSON content for resume.");

            var generateResume = System.Text.Json.JsonSerializer.Deserialize<Resume>(stringGenerateResume);

            if (generateResume == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize the resume JSON.");
                throw new Exception("Failed to deserialize the resume from OpenAI response.");
            }

            _logger.LogInformation("✅ Resume generated successfully.");
            return generateResume;
        }

        public async Task<CoverLetter> CreateCoverLetter(JobOffer jobOffer, Resume resume)
        {
            _logger.LogInformation("🚀 Starting cover letter generation...");

            var stringJobOffer = JsonConvert.SerializeObject(jobOffer, Formatting.Indented);
            var stringResume = JsonConvert.SerializeObject(resume, Formatting.Indented);

            var prompt = PrompHelpers.GenerateCoverLetterPrompt(stringJobOffer, stringResume);
            _logger.LogDebug("Generated cover letter prompt: {Prompt}", prompt);

            var stringCoverLetter = await _openAIClient.GetChatCompletionAsync(prompt);
            _logger.LogDebug("Received cover letter JSON response.");

            stringCoverLetter = StringHelpers.ExtractJsonContent(stringCoverLetter);
            _logger.LogDebug("Extracted JSON content for cover letter.");

            var coverLetter = System.Text.Json.JsonSerializer.Deserialize<CoverLetter>(stringCoverLetter);

            if (coverLetter == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize the cover letter JSON.");
                throw new Exception("Failed to deserialize the cover letter from OpenAI response.");
            }

            _logger.LogInformation("✅ Cover letter generated successfully.");
            return coverLetter;
        }
    }
}
