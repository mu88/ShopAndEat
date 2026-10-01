using Microsoft.Extensions.AI;
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace ShoppingAgent.Services;

public interface IConversationManager
{
    WorkflowPhase Phase { get; }

    void ResetWorkflow();

    ConversationProcessingResult ProcessAsync(
        IReadOnlyList<ChatMessage> conversationHistory,
        IChatClient chatClient,
        Func<IReadOnlyList<AITool>> getTools,
        string shopKey,
        CancellationToken cancellationToken = default);
}
