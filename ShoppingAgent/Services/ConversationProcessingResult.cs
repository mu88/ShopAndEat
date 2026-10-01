using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public sealed class ConversationProcessingResult(
    IAsyncEnumerable<string> chunks,
    IReadOnlyList<ChatMessage> newMessages)
{
    public IAsyncEnumerable<string> Chunks { get; } = chunks;

    public IReadOnlyList<ChatMessage> NewMessages { get; } = newMessages;
}
