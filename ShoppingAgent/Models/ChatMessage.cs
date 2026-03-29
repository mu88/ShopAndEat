namespace ShoppingAgent.Models;

/// <summary>
/// Represents a single message in the shopping agent chat conversation.
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Indicates whether this message is currently being streamed from the LLM.</summary>
    public bool IsStreaming { get; set; }
}
