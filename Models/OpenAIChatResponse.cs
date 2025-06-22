namespace Models
{
    using System.Text.Json.Serialization;

    public class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; set; }
    }
}
