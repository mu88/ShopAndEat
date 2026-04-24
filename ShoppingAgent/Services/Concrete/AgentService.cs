using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Models;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Thin orchestrator that delegates to specialised services for prompt building,
/// tool definitions, tool dispatch, conversation management, and shop session handling.
/// </summary>
public class AgentService(
    IMistralChatClientProvider chatClientProvider,
    ISystemPromptBuilder systemPromptBuilder,
    IToolDefinitionProvider toolDefinitionProvider,
    IConversationManager conversationManager,
    IShopSessionManager shopSessionManager,
    ShoppingAgentMetrics metrics,
    ILogger<AgentService> logger) : IAgentService
{
    private readonly List<Microsoft.Extensions.AI.ChatMessage> _conversationHistory = [];
    private readonly List<Models.ChatMessage> _messages = [];
    private bool _isProcessing;

#pragma warning disable MA0046
    public event Action OnStateChanged;
#pragma warning restore MA0046

    public bool IsProcessing => _isProcessing;

    public IList<Models.ChatMessage> Messages => _messages;

    public string SelectedShopKey => shopSessionManager.SelectedShopKey;
    public IReadOnlyList<ShopConfig> AvailableShops => shopSessionManager.AvailableShops;

    public async Task InitializeAsync(string shopKey = null, CancellationToken cancellationToken = default)
    {
        conversationManager.ResetWorkflow();

        if (shopSessionManager.IsInitialized && string.Equals(shopKey, shopSessionManager.SelectedShopKey, StringComparison.Ordinal))
        {
            return;
        }

        shopSessionManager.SelectShop(shopKey);

        var shop = shopSessionManager.SelectedShop;
        var systemPrompt = await systemPromptBuilder.BuildSystemPromptAsync(
            shop.Name, shop.BaseUrl, shopSessionManager.SelectedShopKey, cancellationToken);

        _conversationHistory.Clear();
        _conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemPrompt));
    }

    public async Task SwitchShopAsync(string shopKey, CancellationToken cancellationToken = default)
    {
        AgentLogMessages.SwitchingShop(logger, shopKey);
        shopSessionManager.Reset();
        _messages.Clear();
        await InitializeAsync(shopKey, cancellationToken);
    }

    public async IAsyncEnumerable<string> ProcessMessageAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!shopSessionManager.IsInitialized)
        {
            await InitializeAsync(cancellationToken: cancellationToken);
        }

        _isProcessing = true;
        OnStateChanged?.Invoke();

        // When the user responds to clarification questions, reset back to Researching
        // so search_products becomes available again for the LLM to complete the plan.
        if (conversationManager.Phase == WorkflowPhase.AwaitingClarification)
        {
            conversationManager.ResetWorkflow();
        }

        _conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userMessage));

        var chatClient = await chatClientProvider.GetChatClientAsync();
        var shopName = shopSessionManager.SelectedShop.Name;

        metrics.MessagesProcessed.Add(1);

        try
        {
            await foreach (var chunk in conversationManager.ProcessAsync(
                _conversationHistory,
                chatClient,
                () => toolDefinitionProvider.GetToolDefinitions(shopName, conversationManager.Phase),
                shopSessionManager.SelectedShopKey,
                cancellationToken))
            {
                yield return chunk;
            }
        }
        finally
        {
            _isProcessing = false;
            OnStateChanged?.Invoke();
        }
    }
}
