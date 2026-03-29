namespace ShoppingAgent.Models;

/// <summary>
/// A tool call request from the LLM to be executed by the extension.
/// </summary>
public record ToolRequest
{
    public string ToolName { get; init; } = string.Empty;

    public Dictionary<string, object> Arguments { get; init; } = new();
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
