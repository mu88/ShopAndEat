using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

/// <summary>
/// Provides access to the current <see cref="IChatClient"/> for Mistral AI.
/// </summary>
public interface IMistralChatClientProvider
{
    /// <summary>
    /// Gets the current chat client, creating one from the configured options if needed.
    /// </summary>
    Task<IChatClient> GetChatClientAsync();

    /// <summary>
    /// Invalidates the cached client so the next call to <see cref="GetChatClientAsync"/> creates a new one.
    /// </summary>
    void InvalidateClient();

    /// <summary>
    /// Checks connectivity by calling GET /models — no token cost.
    /// Returns <c>true</c> when the API key is valid and the endpoint is reachable.
    /// </summary>
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);
}
