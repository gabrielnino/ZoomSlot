using System.Net.Http.Json;
using Configuration;
using Models;
using Services.Interfaces;

namespace Services
{
    public class OpenAIClient : IOpenAIClient
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new();  // Single shared HttpClient

        public OpenAIClient(AppConfig appConfig)
        {
            _apiKey = Environment.GetEnvironmentVariable(appConfig.Llm.ApiKey, EnvironmentVariableTarget.Machine)
                     ?? throw new ArgumentException("API key cannot be null or whitespace.", nameof(appConfig.Llm.ApiKey));

            _httpClient.BaseAddress ??= new Uri(appConfig.Llm.Url);
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<string> GetChatCompletionAsync(Prompt prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt.SystemContent) || string.IsNullOrWhiteSpace(prompt.UserContent))
                throw new ArgumentException("Prompt content cannot be null or whitespace.");

            var request = new OpenAIChatRequest
            {
                Model = "deepseek-chat",
                Messages = [
                    new() { Role = "system", Content = prompt.SystemContent },
                new() { Role = "user", Content = prompt.UserContent }
                ]
            };

            var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API request failed with status code {response.StatusCode}: {errorContent}");
            }

            var responseData = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            if (responseData?.Choices == null || responseData.Choices.Count == 0)
            {
                throw new Exception("No response received from OpenAI API.");
            }

            return responseData.Choices[0].Message.Content.Trim();
        }
    }
}
