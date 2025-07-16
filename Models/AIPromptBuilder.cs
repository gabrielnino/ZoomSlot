using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Models;

public class AIPromptBuilder
{
    public static string StepTag
    {
        get { return "###ResultPreviousStep##"; }
    }
    public required string Role { get; set; } = "assistant";
    public required string Task { get; set; }
    public required string Context { get; set; }
    public required string Format { get; set; }
    public string Tone { get; set; } = "professional";
    public string Style { get; set; } = "concise";
    public int? MaxLength { get; set; }
    public bool IncludeSources { get; set; }
    public bool StepByStep { get; set; }
    public int? Step
    {
        get
        {
            return Parent == null ? 1 : Parent.Step + 1; ;
        }
    }
    public List<string> Examples { get; } = [];
    public List<string> Constraints { get; } = [];
    public Dictionary<string, string> AdditionalParameters { get; } = [];
    public List<ChatMessage> ConversationHistory { get; } = [];
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; }

        [JsonPropertyName("content")]
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
    public AIPromptBuilder? Parent { get; private set; }

    public void SetParent(AIPromptBuilder parent)
    {
        Parent = parent;
    }
    private AIPromptBuilder? _nextTask;
    public AIPromptBuilder? NextTask
    {
        get => _nextTask;
        set
        {
            _nextTask = value;
            _nextTask?.SetParent(this);
        }
    }
    public void AddExample(string example) => Examples.Add(example);
    public void AddConstraint(string constraint) => Constraints.Add(constraint);
    public void AddParameter(string key, string value) => AdditionalParameters[key] = value;
    public void AddToConversationHistory(string role, string content) => ConversationHistory.Add(new ChatMessage(role, content));
    public void ClearConversationHistory() => ConversationHistory.Clear();
    public string BuildPrompt()
    {
        var sb = new StringBuilder();
        AppendLineIfNotNull(sb, "Role", Role);
        AppendLineIfNotNull(sb, "Task", Task);
        AppendLineIfNotNull(sb, "Context", Context);
        AppendLineIfNotNull(sb, "Response Format", Format);
        AppendList(sb, "Examples", Examples);
        AppendList(sb, "Constraints", Constraints);
        sb.AppendLine($"Tone: {Tone}");
        sb.AppendLine($"Style: {Style}");
        if (MaxLength.HasValue)
        {
            sb.AppendLine($"Maximum length: {MaxLength} words");
        }
        if (IncludeSources)
        {
            sb.AppendLine("Include relevant sources or references.");
        }
        if (StepByStep)
        {
            sb.AppendLine("Provide step-by-step response.");
        }
        if (AdditionalParameters.Count > 0)
        {
            sb.AppendLine("Additional Parameters:");
            foreach (var (key, value) in AdditionalParameters)
            {
                sb.AppendLine($"{key}: {value}");
            }
        }
        return sb.ToString();
    }
    public Prompt BuildPromptObject(string? result = null)
    {
        var systemBuilder = new StringBuilder();
        AppendLineIfNotNull(systemBuilder, "Task", Task);
        AppendLineIfNotNull(systemBuilder, "Context", Context);
        AppendList(systemBuilder, "Constraints", Constraints);

        var userBuilder = new StringBuilder();
        AppendLineIfNotNull(userBuilder, "Role", Role);
        AppendLineIfNotNull(userBuilder, "Response Format", Format);
        AppendList(userBuilder, "Examples", Examples);
        userBuilder.AppendLine($"Tone: {Tone}");
        userBuilder.AppendLine($"Style: {Style}");

        if (MaxLength.HasValue)
        {
            userBuilder.AppendLine($"Maximum length: {MaxLength} words");
        }
        if (IncludeSources)
        {
            userBuilder.AppendLine("Include relevant sources or references.");
        }
        if (StepByStep)
        {
            userBuilder.AppendLine("Provide step-by-step response.");
        }
        if (AdditionalParameters.Count > 0)
        {
            userBuilder.AppendLine("Additional Parameters:");
            foreach (var (key, value) in AdditionalParameters)
            {
                userBuilder.AppendLine($"{key}: {value}");
            }
        }

        return new Prompt
        {
            SystemContent = systemBuilder.ToString().Trim().Replace(AIPromptBuilder.StepTag, result ?? string.Empty),
            UserContent = userBuilder.ToString().Trim(),
        };
    }
    public List<ChatMessage> GetApiMessages()
    {
        var messages = new List<ChatMessage>();
        var systemBuilder = new StringBuilder();
        AppendLineIfNotNull(systemBuilder, "Task", Task);
        AppendLineIfNotNull(systemBuilder, "Context", Context);
        AppendList(systemBuilder, "Constraints", Constraints);
        if (systemBuilder.Length > 0)
        {
            messages.Add(new ChatMessage("system", systemBuilder.ToString()));
        }
        if (Examples.Count > 0)
        {
            var examplesText = string.Join(Environment.NewLine, Examples.Select(e => $"- {e}"));
            messages.Add(new ChatMessage("user", $"Examples:\n{examplesText}"));
        }
        messages.Add(new ChatMessage("user", BuildPrompt()));
        messages.AddRange(ConversationHistory);
        return messages;
    }

    public string GetApiRequestJson(bool includeSystemMessage = true)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        return JsonSerializer.Serialize(new { messages = GetApiMessages() }, options);
    }

    private static void AppendLineIfNotNull(StringBuilder sb, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}: {value}");
        }
    }

    private static void AppendList(StringBuilder sb, string title, List<string> items)
    {
        if (items?.Count > 0)
        {
            sb.AppendLine($"{title}:");
            foreach (var item in items)
                sb.AppendLine($"- {item}");
        }
    }
}
