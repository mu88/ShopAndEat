namespace ShoppingAgent.Services;

public sealed class ConversationProcessingState
{
    public bool ShouldBreak { get; set; }

    public string? RepeatedFailureTool { get; set; }

    public IDictionary<string, int> FailureTracker { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
}
