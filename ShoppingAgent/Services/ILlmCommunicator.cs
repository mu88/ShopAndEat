using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public interface ILlmCommunicator
{
    Task<(ChatResponse? Response, string? ErrorMessage, string? FallbackMessage)> GetResponseAsync(
        IChatClient chatClient,
        IList<ChatMessage> conversationHistory,
        Func<IReadOnlyList<AITool>> getTools,
        CancellationToken cancellationToken = default);
}
