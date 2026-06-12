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

public sealed class LlmRetryPolicyFactory(
    IOptions<LlmClientOptions> llmOptions,
    ShoppingAgentMetrics metrics,
    ILogger<LlmRetryPolicyFactory> logger) : ILlmRetryPolicyFactory
{
    public ResiliencePipeline<ChatResponse> CreateChatResponsePipeline()
        => new ResiliencePipelineBuilder<ChatResponse>()
            .AddRetry(BuildRetryOptions())
            .Build();

    public ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> CreateStreamingStartPipeline()
        => new ResiliencePipelineBuilder<IAsyncEnumerable<ChatResponseUpdate>>()
            .AddRetry(BuildStreamingRetryOptions())
            .Build();

    private RetryStrategyOptions<ChatResponse> BuildRetryOptions()
    {
        var options = llmOptions.Value;
        return new RetryStrategyOptions<ChatResponse>
        {
            ShouldHandle = new PredicateBuilder<ChatResponse>()
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
    }

    private RetryStrategyOptions<IAsyncEnumerable<ChatResponseUpdate>> BuildStreamingRetryOptions()
    {
        var options = llmOptions.Value;
        return new RetryStrategyOptions<IAsyncEnumerable<ChatResponseUpdate>>
        {
            ShouldHandle = new PredicateBuilder<IAsyncEnumerable<ChatResponseUpdate>>()
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
    }

    private int GetMaxRetryAttempts(int totalAttempts) => Math.Max(0, totalAttempts - 1);

    private bool IsRateLimited(ClientResultException ex)
        => ex.Status == (int)HttpStatusCode.TooManyRequests;
}
