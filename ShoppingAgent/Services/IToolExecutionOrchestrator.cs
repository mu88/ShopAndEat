using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public interface IToolExecutionOrchestrator
{
    IAsyncEnumerable<string> ProcessResponseAsync(
        ChatResponse response,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        ConversationProcessingState state,
        CancellationToken cancellationToken = default);
}
