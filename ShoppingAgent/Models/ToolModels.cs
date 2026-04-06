namespace ShoppingAgent.Models;

/// <summary>
/// A tool call request from the LLM to be executed by the extension.
/// </summary>
public record ToolRequest
{
    public string ToolName { get; init; } = string.Empty;

    public IDictionary<string, object> Arguments { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}

/// <summary>
/// Result of a tool execution.
/// </summary>
public record ToolResult
{
    public bool Success { get; init; }

    /// <summary>
    /// The call ID that matches <see cref="ToolRequest"/> so concurrent calls can be correlated.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    public string Data { get; init; } = string.Empty;

    public string Error { get; init; } = string.Empty;
}
