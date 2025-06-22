using System.Net.Http.Json;
using Models;
using Services.Interfaces;

namespace Services
{
    public class OpenAIClient : IOpenAIClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIClient(string apiKey, string uriString, HttpClient? httpClient = null)
        {


            _apiKey = Environment.GetEnvironmentVariable(apiKey, EnvironmentVariableTarget.Machine);

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("API key cannot be null or whitespace.", nameof(_apiKey));
            }

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = new Uri(uriString);
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<string> GetChatCompletionAsync(Prompt prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt.SystemContent) || string.IsNullOrWhiteSpace(prompt.SystemContent))
                throw new ArgumentException("Code cannot be null or whitespace.", nameof(prompt));

            var request = new OpenAIChatRequest
            {
                Model = "deepseek-chat",
                Messages =
                [
                    new() { Role = "system",  Content = prompt.SystemContent },
                    new() { Role = "user",  Content = prompt.UserContent }
                ]
            };

            var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API request failed with status code {response.StatusCode}: {errorContent}");
            }

            var responseData = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();

            if (responseData == null || responseData.Choices == null || responseData.Choices.Count == 0)
            {
                throw new Exception("No response received from OpenAI API.");
            }

            var improvedCode = responseData.Choices[0].Message.Content.Trim();
            return improvedCode;
        }
    }
}
