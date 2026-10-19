using System.ClientModel;
using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.DependencyInjection;
using Polly.Registry;
using Polly.Retry;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;

namespace ShoppingAgent.Services.Concrete;

public sealed class LlmRetryPolicyFactory : ILlmRetryPolicyFactory
{
    internal const string ChatResponsePipelineKey = "llm-chat-response";
    internal const string StreamingStartPipelineKey = "llm-streaming-start";

    private readonly ResiliencePipeline<ChatResponse> _chatResponsePipeline;
    private readonly ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> _streamingStartPipeline;

    public LlmRetryPolicyFactory(ResiliencePipelineProvider<string> pipelineProvider)
    {
        _chatResponsePipeline = pipelineProvider.GetPipeline<ChatResponse>(ChatResponsePipelineKey);
        _streamingStartPipeline = pipelineProvider.GetPipeline<IAsyncEnumerable<ChatResponseUpdate>>(StreamingStartPipelineKey);
    }

    public ResiliencePipeline<ChatResponse> CreateChatResponsePipeline() => _chatResponsePipeline;

    public ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> CreateStreamingStartPipeline() => _streamingStartPipeline;

    internal static RetryStrategyOptions<TResult> BuildRetryOptions<TResult>(
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

public static class LlmResiliencePipelineRegistration
{
    public static IServiceCollection AddLlmResiliencePipelines(this IServiceCollection services)
    {
        services.AddResiliencePipeline<string, ChatResponse>(
            LlmRetryPolicyFactory.ChatResponsePipelineKey,
            ConfigureChatResponsePipeline);
        services.AddResiliencePipeline<string, IAsyncEnumerable<ChatResponseUpdate>>(
            LlmRetryPolicyFactory.StreamingStartPipelineKey,
            ConfigureStreamingStartPipeline);
        return services;
    }

    private static void ConfigureChatResponsePipeline(
        ResiliencePipelineBuilder<ChatResponse> builder,
        AddResiliencePipelineContext<string> context) =>
        ConfigurePipeline(builder, context);

    private static void ConfigureStreamingStartPipeline(
        ResiliencePipelineBuilder<IAsyncEnumerable<ChatResponseUpdate>> builder,
        AddResiliencePipelineContext<string> context) =>
        ConfigurePipeline(builder, context);

    private static void ConfigurePipeline<TResult>(
        ResiliencePipelineBuilder<TResult> builder,
        AddResiliencePipelineContext<string> context)
    {
        var serviceProvider = context.ServiceProvider;
        var options = serviceProvider.GetRequiredService<IOptions<LlmClientOptions>>().Value;
        var metrics = serviceProvider.GetRequiredService<ShoppingAgentMetrics>();
        var logger = serviceProvider.GetRequiredService<ILogger<LlmRetryPolicyFactory>>();
        builder.AddRetry(LlmRetryPolicyFactory.BuildRetryOptions<TResult>(options, metrics, logger));
    }
}
