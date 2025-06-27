using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class AIPromptBuilder
{
    // Core prompt components
    public string Role { get; set; } = "assistant";
    public string Task { get; set; }
    public string Context { get; set; }
    public string Format { get; set; }
    public List<string> Examples { get; private set; } = [];
    public List<string> Constraints { get; private set; } = [];
    public Dictionary<string, string> AdditionalParameters { get; private set; } = [];

    // Tone and style
    public string Tone { get; set; } = "professional";
    public string Style { get; set; } = "concise";

    // Response characteristics
    public int? MaxLength { get; set; }
    public bool IncludeSources { get; set; }
    public bool StepByStep { get; set; }

    // For conversation history
    public List<ChatMessage> ConversationHistory { get; private set; } = [];

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    /// <summary>
    /// Builds the complete prompt as a single content string
    /// </summary>
    public string BuildPrompt()
    {
        var prompt = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(Role))
        {
            prompt.AppendLine($"Role: {Role}");
        }

        if (!string.IsNullOrWhiteSpace(Task))
        {
            prompt.AppendLine($"Task: {Task}");
        }

        if (!string.IsNullOrWhiteSpace(Context))
        {
            prompt.AppendLine($"Context: {Context}");
        }

        if (!string.IsNullOrWhiteSpace(Format))
        {
            prompt.AppendLine($"Response Format: {Format}");
        }

        if (Examples.Count > 0)
        {
            prompt.AppendLine("Examples:");
            foreach (var example in Examples)
            {
                prompt.AppendLine($"- {example}");
            }
        }

        if (Constraints.Count > 0)
        {
            prompt.AppendLine("Constraints:");
            foreach (var constraint in Constraints)
            {
                prompt.AppendLine($"- {constraint}");
            }
        }

        prompt.AppendLine($"Tone: {Tone}");
        prompt.AppendLine($"Style: {Style}");

        if (MaxLength.HasValue)
        {
            prompt.AppendLine($"Maximum length: {MaxLength} words");
        }

        if (IncludeSources)
        {
            prompt.AppendLine("Include relevant sources or references.");
        }

        if (StepByStep)
        {
            prompt.AppendLine("Provide step-by-step response.");
        }

        if (AdditionalParameters.Count > 0)
        {
            prompt.AppendLine("Additional Parameters:");
            foreach (var param in AdditionalParameters)
            {
                prompt.AppendLine($"{param.Key}: {param.Value}");
            }
        }

        return prompt.ToString();
    }

    /// <summary>
    /// Gets the messages formatted for API request with role/content structure
    /// </summary>
    public List<ChatMessage> GetApiMessages()
    {
        var messages = new List<ChatMessage>();

        // Add system message with instructions
        var systemMessage = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(Task)) systemMessage.AppendLine($"Task: {Task}");
        if (!string.IsNullOrWhiteSpace(Context)) systemMessage.AppendLine($"Context: {Context}");
        if (Constraints.Count > 0)
        {
            systemMessage.AppendLine("Constraints:");
            foreach (var constraint in Constraints)
            {
                systemMessage.AppendLine($"- {constraint}");
            }
        }
        if (systemMessage.Length > 0)
        {
            messages.Add(new ChatMessage("system", systemMessage.ToString()));
        }

        // Add examples as separate messages
        if (Examples.Count > 0)
        {
            var examplesMessage = new StringBuilder("Examples:\n");
            foreach (var example in Examples)
            {
                examplesMessage.AppendLine($"- {example}");
            }
            messages.Add(new ChatMessage("user", examplesMessage.ToString()));
        }

        // Add the main prompt content
        messages.Add(new ChatMessage("user", BuildPrompt()));

        // Add conversation history
        if (ConversationHistory.Count > 0)
        {
            messages.AddRange(ConversationHistory);
        }

        return messages;
    }

    /// <summary>
    /// Gets the API request payload as JSON
    /// </summary>
    public string GetApiRequestJson(bool includeSystemMessage = true)
    {
        var messages = GetApiMessages();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        return JsonSerializer.Serialize(new { messages }, options);
    }

    /// <summary>
    /// Adds an example to the prompt
    /// </summary>
    public void AddExample(string example)
    {
        Examples.Add(example);
    }

    /// <summary>
    /// Adds a constraint to the prompt
    /// </summary>
    public void AddConstraint(string constraint)
    {
        Constraints.Add(constraint);
    }

    /// <summary>
    /// Adds a parameter to the prompt
    /// </summary>
    public void AddParameter(string key, string value)
    {
        AdditionalParameters[key] = value;
    }

    /// <summary>
    /// Adds a message to the conversation history
    /// </summary>
    public void AddToConversationHistory(string role, string content)
    {
        ConversationHistory.Add(new ChatMessage(role, content));
    }

    /// <summary>
    /// Clears the conversation history
    /// </summary>
    public void ClearConversationHistory()
    {
        ConversationHistory.Clear();
    }
}