using System.ClientModel;
using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;

namespace ShoppingAgent.Services.Concrete;

public sealed class LlmRetryPolicyFactory : ILlmRetryPolicyFactory
{
    // Polly's ResiliencePipeline<T> is immutable and safe for concurrent reuse once built, and the
    // options it is built from (IOptions<T>.Value, not IOptionsMonitor) are fixed for the lifetime of
    // this singleton. Building each pipeline once here (instead of on every Create*Pipeline call) avoids
    // reallocating the same pipeline on every chat message.
    private readonly ResiliencePipeline<ChatResponse> _chatResponsePipeline;
    private readonly ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> _streamingStartPipeline;

    public LlmRetryPolicyFactory(
        IOptions<LlmClientOptions> llmOptions,
        ShoppingAgentMetrics metrics,
        ILogger<LlmRetryPolicyFactory> logger)
    {
        var options = llmOptions.Value;
        _chatResponsePipeline = CreatePipeline<ChatResponse>(options, metrics, logger);
        _streamingStartPipeline = CreatePipeline<IAsyncEnumerable<ChatResponseUpdate>>(options, metrics, logger);
    }

    public ResiliencePipeline<ChatResponse> CreateChatResponsePipeline() => _chatResponsePipeline;

    public ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> CreateStreamingStartPipeline() => _streamingStartPipeline;

    private static ResiliencePipeline<TResult> CreatePipeline<TResult>(
        LlmClientOptions options,
        ShoppingAgentMetrics metrics,
        ILogger logger)
        => new ResiliencePipelineBuilder<TResult>()
            .AddRetry(BuildRetryOptions<TResult>(options, metrics, logger))
            .Build();

    private static RetryStrategyOptions<TResult> BuildRetryOptions<TResult>(
        LlmClientOptions options,
        ShoppingAgentMetrics metrics,
        ILogger logger)
        => new()
        {
            ShouldHandle = new PredicateBuilder<TResult>()
                .Handle<ClientResultException>(IsRateLimited),
            MaxRetryAttempts = GetMaxRetryAttempts(options.RetryMaxAttempts),
            Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                metrics.RetriesTotal.Add(1);
                var delayMs = (int)args.RetryDelay.TotalMilliseconds;
                var attempt = args.AttemptNumber + 1;
                AgentLogMessages.RateLimitedRetrying(logger, delayMs, attempt, options.RetryMaxAttempts);
                return ValueTask.CompletedTask;
            },
        };

    private static int GetMaxRetryAttempts(int totalAttempts) => Math.Max(0, totalAttempts - 1);

    private static bool IsRateLimited(ClientResultException ex)
        => ex.Status == (int)HttpStatusCode.TooManyRequests;
}
