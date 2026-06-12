using System.ClientModel;
using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Polly;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Wraps an IChatClient with resilience logic: retry on 429 with exponential backoff,
/// and optional fallback to a secondary model when retries are exhausted.
/// </summary>
public sealed class ResilientChatClient : IChatClient
{
    private readonly IChatClient _primaryClient;
    private readonly IChatClient _fallbackClient;
    private readonly ILogger<ResilientChatClient> _logger;
    private readonly LlmClientOptions _llmOptions;
    private readonly AgentOptions _agentOptions;
    private readonly ShoppingAgentMetrics _metrics;
    private readonly ResiliencePipeline<ChatResponse> _chatResponsePipeline;
    private readonly ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> _streamingStartPipeline;

    public ResilientChatClient(
        IChatClient primaryClient,
        IChatClient fallbackClient,
        ILogger<ResilientChatClient> logger,
        LlmClientOptions llmOptions,
        AgentOptions agentOptions,
        ShoppingAgentMetrics metrics,
        ILlmRetryPolicyFactory retryPolicyFactory)
    {
        _primaryClient = primaryClient;
        _fallbackClient = fallbackClient;
        _logger = logger;
        _llmOptions = llmOptions;
        _agentOptions = agentOptions;
        _metrics = metrics;
        _chatResponsePipeline = retryPolicyFactory.CreateChatResponsePipeline();
        _streamingStartPipeline = retryPolicyFactory.CreateStreamingStartPipeline();
    }

    public object GetService(Type serviceType, object serviceKey = null)
    {
        return _primaryClient.GetService(serviceType, serviceKey);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_agentOptions.RetryEnabled)
        {
            return await _primaryClient.GetResponseAsync(chatMessages, options, cancellationToken);
        }

        var messages = chatMessages.ToList();
        try
        {
            return await _chatResponsePipeline.ExecuteAsync(
                async token => await _primaryClient.GetResponseAsync(messages, options, token),
                cancellationToken);
        }
        catch (ClientResultException ex) when (IsRateLimited(ex))
        {
            return await HandleRetriesExhaustedAsync(messages, options, ex, cancellationToken);
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_agentOptions.RetryEnabled)
        {
            await foreach (var chunk in _primaryClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return chunk;
            }

            yield break;
        }

        var messages = chatMessages.ToList();
        IAsyncEnumerable<ChatResponseUpdate> response;
        try
        {
            response = await _streamingStartPipeline.ExecuteAsync(
                token => ValueTask.FromResult(_primaryClient.GetStreamingResponseAsync(messages, options, token)),
                cancellationToken);
        }
        catch (ClientResultException ex) when (IsRateLimited(ex))
        {
            response = await HandleRetriesExhaustedStreamAsync(messages, options, ex, cancellationToken);
        }

        if (response is not null)
        {
            await foreach (var chunk in response)
            {
                yield return chunk;
            }
        }
    }

    public void Dispose()
    {
    }

    private static bool IsRateLimited(ClientResultException ex)
    {
        return ex.Status == (int)HttpStatusCode.TooManyRequests;
    }

    private async Task<ChatResponse> HandleRetriesExhaustedAsync(
        List<ChatMessage> messages,
        ChatOptions options,
        ClientResultException lastException,
        CancellationToken cancellationToken)
    {
        if (_agentOptions.ModelFallbackEnabled && _fallbackClient is not null && lastException is not null)
        {
            AgentLogMessages.RetriesExhausted(_logger, _llmOptions.RetryMaxAttempts);
            AgentLogMessages.FallbackStarted(_logger);
            _metrics.ModelFallbacksTotal.Add(1);

            try
            {
                var response = await _fallbackClient.GetResponseAsync(messages, options, cancellationToken);
                AgentLogMessages.FallbackSucceeded(_logger);
                return response;
            }
            catch (Exception fallbackEx)
            {
                AgentLogMessages.FallbackFailed(_logger, fallbackEx.Message);
                throw;
            }
        }

        if (lastException is not null)
        {
            AgentLogMessages.RetriesExhausted(_logger, _llmOptions.RetryMaxAttempts);
            throw lastException;
        }

        throw new InvalidOperationException("Unexpected state in ResilientChatClient");
    }

    private async Task<IAsyncEnumerable<ChatResponseUpdate>> HandleRetriesExhaustedStreamAsync(
        List<ChatMessage> messages,
        ChatOptions options,
        ClientResultException lastException,
        CancellationToken cancellationToken)
    {
        if (_agentOptions.ModelFallbackEnabled && _fallbackClient is not null && lastException is not null)
        {
            AgentLogMessages.RetriesExhausted(_logger, _llmOptions.RetryMaxAttempts);
            AgentLogMessages.FallbackStarted(_logger);
            _metrics.ModelFallbacksTotal.Add(1);

            return HandleFallbackStreamAsync(messages, options, cancellationToken);
        }

        if (lastException is not null)
        {
            AgentLogMessages.RetriesExhausted(_logger, _llmOptions.RetryMaxAttempts);
            throw lastException;
        }

        throw new InvalidOperationException("Unexpected state in ResilientChatClient");
    }

    private async IAsyncEnumerable<ChatResponseUpdate> HandleFallbackStreamAsync(
        List<ChatMessage> messages,
        ChatOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        bool succeeded = false;

        try
        {
            await foreach (var chunk in _fallbackClient.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return chunk;
            }

            succeeded = true;
        }
        finally
        {
            if (succeeded)
            {
                AgentLogMessages.FallbackSucceeded(_logger);
            }
            else
            {
                AgentLogMessages.FallbackFailed(_logger, "Stream interrupted");
            }
        }
    }
}
