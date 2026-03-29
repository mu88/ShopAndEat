using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Creates and caches an <see cref="IChatClient"/> for Mistral AI.
/// The API key is stored in memory only (never persisted to browser storage).
/// </summary>
public class MistralChatClientProvider : IMistralChatClientProvider
{
    private IChatClient _cachedClient;
    private string _currentApiKey;
    private readonly HttpClient _http;
    private readonly ILogger<MistralChatClientProvider> _logger;
    private readonly LlmClientOptions _llmOptions;

    public MistralChatClientProvider(HttpClient http, ILogger<MistralChatClientProvider> logger, IOptions<LlmClientOptions> llmOptions)
    {
        _http = http;
        _logger = logger;
        _llmOptions = llmOptions.Value;
    }

    public string ApiKey
    {
        get => _currentApiKey;
        set
        {
            if (_currentApiKey == value) return;
            _currentApiKey = value;
            InvalidateClient();
        }
    }

    /// <summary>
    /// Whether a valid API key has been provided.
    /// </summary>
    public bool HasApiKey => !string.IsNullOrEmpty(_currentApiKey);

    public Task<IChatClient> GetChatClientAsync()
    {
        if (_cachedClient != null)
        {
            return Task.FromResult(_cachedClient);
        }

        if (!HasApiKey)
        {
            throw new InvalidOperationException("Mistral API key not set.");
        }

        ServiceLogMessages.CreatingChatClient(_logger, _llmOptions.DefaultModel);
        var options = new OpenAIClientOptions { Endpoint = new Uri(_llmOptions.Endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(_currentApiKey), options);
        _cachedClient = client.GetChatClient(_llmOptions.DefaultModel).AsIChatClient();

        return Task.FromResult(_cachedClient);
    }

    /// <summary>
    /// Validates the current API key by listing available models (no token cost).
    /// Returns null on success, or an error message on failure.
    /// On failure, the key and cached client are cleared.
    /// </summary>
    public async Task<string> ValidateKeyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_llmOptions.Endpoint}/models");
            request.Headers.Add("Authorization", $"Bearer {_currentApiKey}");
            var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception ex)
        {
            InvalidateClient();
            _currentApiKey = null;
            ServiceLogMessages.ApiKeyValidationFailed(_logger, ex.Message);
            return ex.Message;
        }
    }

    public void InvalidateClient()
    {
        if (_cachedClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_cachedClient != null)
        {
            ServiceLogMessages.ApiKeyInvalidated(_logger);
        }

        _cachedClient = null;
    }

    public void ClearApiKey()
    {
        _currentApiKey = null;
        InvalidateClient();
    }
}
