using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

/// <summary>
/// Provides access to the current <see cref="IChatClient"/> for Mistral AI, recreating it when settings change.
/// </summary>
public interface IMistralChatClientProvider
{
    /// <summary>
    /// Gets the current chat client, creating one from the active settings if needed.
    /// </summary>
    Task<IChatClient> GetChatClientAsync();

    /// <summary>
    /// Gets or sets the API key. Setting a new key invalidates the cached client.
    /// </summary>
    string ApiKey { get; set; }

    /// <summary>
    /// Whether a valid API key has been provided.
    /// </summary>
    bool HasApiKey { get; }

    /// <summary>
    /// Invalidates the cached client so the next call to <see cref="GetChatClientAsync"/> creates a new one.
    /// </summary>
    void InvalidateClient();

    /// <summary>
    /// Clears the current API key and invalidates the cached client.
    /// </summary>
    void ClearApiKey();

    /// <summary>
    /// Validates the current API key by making a lightweight API call.
    /// Returns null on success, or an error message on failure.
    /// On failure, the key and cached client are cleared.
    /// </summary>
    Task<string> ValidateKeyAsync(CancellationToken cancellationToken = default);
}
