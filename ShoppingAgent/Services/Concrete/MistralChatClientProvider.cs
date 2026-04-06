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
/// The API key is read from <see cref="LlmClientOptions.ApiKey"/> which is loaded
/// server-side from a Docker Secret, environment variable, or appsettings.
/// </summary>
public sealed class MistralChatClientProvider : IMistralChatClientProvider, IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<MistralChatClientProvider> _logger;
    private readonly LlmClientOptions _llmOptions;
    private IChatClient _cachedClient;

    public MistralChatClientProvider(HttpClient http, ILogger<MistralChatClientProvider> logger, IOptions<LlmClientOptions> llmOptions)
    {
        _http = http;
        _logger = logger;
        _llmOptions = llmOptions.Value;
    }

    public Task<IChatClient> GetChatClientAsync()
    {
        if (_cachedClient is not null)
        {
            return Task.FromResult(_cachedClient);
        }

        ServiceLogMessages.CreatingChatClient(_logger, _llmOptions.DefaultModel);
        var options = new OpenAIClientOptions { Endpoint = new Uri(_llmOptions.Endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(_llmOptions.ApiKey), options);
#pragma warning disable IDISP003 // _cachedClient is null at this point (early return above)
        _cachedClient = client.GetChatClient(_llmOptions.DefaultModel).AsIChatClient();
#pragma warning restore IDISP003

        return Task.FromResult(_cachedClient);
    }

    public void InvalidateClient()
    {
        var wasInitialized = _cachedClient is not null;
        (_cachedClient as IDisposable)?.Dispose();
        _cachedClient = null;

        if (wasInitialized)
        {
            ServiceLogMessages.ApiKeyInvalidated(_logger);
        }
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_llmOptions.Endpoint}/models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _llmOptions.ApiKey);
            using var response = await _http.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LlmConnectionCheckFailed(_logger, ex);
            return false;
        }
    }

    public void Dispose()
    {
        (_cachedClient as IDisposable)?.Dispose();
        _cachedClient = null;
    }
}
