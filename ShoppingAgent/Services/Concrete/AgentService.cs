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
    private readonly List<Microsoft.Extensions.AI.ChatMessage> _conversationHistory = new();
    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;
    public List<Models.ChatMessage> Messages { get; } = new();
    public event Action OnStateChanged;

    public string SelectedShopKey => shopSessionManager.SelectedShopKey;
    public IReadOnlyList<ShopConfig> AvailableShops => shopSessionManager.AvailableShops;

    public async Task InitializeAsync(string shopKey = null, CancellationToken cancellationToken = default)
    {
        if (shopSessionManager.IsInitialized && shopKey == shopSessionManager.SelectedShopKey)
            return;

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
        Messages.Clear();
        await InitializeAsync(shopKey, cancellationToken);
    }

    public async IAsyncEnumerable<string> ProcessMessageAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!shopSessionManager.IsInitialized)
            await InitializeAsync(cancellationToken: cancellationToken);

        _isProcessing = true;
        OnStateChanged?.Invoke();

        _conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userMessage));

        var chatClient = await chatClientProvider.GetChatClientAsync();
        var tools = toolDefinitionProvider.GetToolDefinitions(shopSessionManager.SelectedShop.Name);

        metrics.MessagesProcessed.Add(1);

        try
        {
            await foreach (var chunk in conversationManager.ProcessAsync(
                _conversationHistory, chatClient, tools, shopSessionManager.SelectedShopKey, cancellationToken))
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
