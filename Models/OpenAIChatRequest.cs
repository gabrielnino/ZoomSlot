namespace Models
{
    using System.Text.Json.Serialization;

    public class OpenAIChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; }
    }
}
