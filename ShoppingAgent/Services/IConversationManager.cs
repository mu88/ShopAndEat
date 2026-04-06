using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public interface IConversationManager
{
    IAsyncEnumerable<string> ProcessAsync(
        IList<ChatMessage> conversationHistory,
        IChatClient chatClient,
        IReadOnlyList<AITool> tools,
        string shopKey,
        CancellationToken cancellationToken = default);
}
