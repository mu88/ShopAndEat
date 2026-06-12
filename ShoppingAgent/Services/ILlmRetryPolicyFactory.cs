using Microsoft.Extensions.AI;
using Polly;

namespace ShoppingAgent.Services;

public interface ILlmRetryPolicyFactory
{
    ResiliencePipeline<ChatResponse> CreateChatResponsePipeline();

    ResiliencePipeline<IAsyncEnumerable<ChatResponseUpdate>> CreateStreamingStartPipeline();
}
