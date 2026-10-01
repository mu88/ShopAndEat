using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Services;

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
    ILlmRetryPolicyFactory retryPolicyFactory,
    IOptions<LlmClientOptions> llmOptions,
    IOptions<AgentOptions> agentOptions,
    ShoppingAgentMetrics metrics,
    ILogger<AgentService> logger,
    ILogger<ResilientChatClient> resilientLogger) : IAgentService
{
    private readonly List<Microsoft.Extensions.AI.ChatMessage> _conversationHistory = [];
    private readonly List<Models.ChatMessage> _messages = [];
    private bool _isProcessing;

#pragma warning disable MA0046
    public event Action? OnStateChanged;
#pragma warning restore MA0046

    public bool IsProcessing => _isProcessing;

    public IReadOnlyList<Models.ChatMessage> Messages => _messages;

    public string? SelectedShopKey => shopSessionManager.SelectedShopKey;
    public IReadOnlyList<ShopConfig> AvailableShops => shopSessionManager.AvailableShops;

    public void AddMessage(Models.ChatMessage message) => _messages.Add(message);

    public async Task InitializeAsync(string? shopKey = null, CancellationToken cancellationToken = default)
    {
        conversationManager.ResetWorkflow();

        if (shopSessionManager.IsInitialized && string.Equals(shopKey, shopSessionManager.SelectedShopKey, StringComparison.Ordinal))
        {
            return;
        }

        shopSessionManager.SelectShop(shopKey);

        var shop = shopSessionManager.SelectedShop!;
        var systemPrompt = await systemPromptBuilder.BuildSystemPromptAsync(
            shop.Name, shop.BaseUrl, shopSessionManager.SelectedShopKey!, cancellationToken);

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
        await EnsureInitializedAsync(cancellationToken);
        BeginProcessing();

        _conversationHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userMessage));

        var (resilientClient, shopName) = await PrepareResilientClientAsync();

        var result = conversationManager.ProcessAsync(
            _conversationHistory,
            resilientClient,
            () => toolDefinitionProvider.GetToolDefinitions(shopName, conversationManager.Phase),
            shopSessionManager.SelectedShopKey!,
            cancellationToken);

        try
        {
            await foreach (var chunk in result.Chunks.WithCancellation(cancellationToken))
            {
                yield return chunk;
            }
        }
        finally
        {
            // Appending here (not only on the happy path) ensures that messages already produced
            // before a cancellation/exception - including tool calls that already ran with real
            // side effects - are still recorded in the conversation history instead of being lost.
            _conversationHistory.AddRange(result.NewMessages);
            _isProcessing = false;
            OnStateChanged?.Invoke();
        }
    }

    private async Task<(ResilientChatClient Client, string ShopName)> PrepareResilientClientAsync()
    {
#pragma warning disable IDISP001 // Dispose created
        var resilientClient = await CreateResilientClientAsync();
#pragma warning restore IDISP001
        var shopName = shopSessionManager.SelectedShop!.Name;

        metrics.MessagesProcessed.Add(1);

        return (resilientClient, shopName);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!shopSessionManager.IsInitialized)
        {
            await InitializeAsync(cancellationToken: cancellationToken);
        }
    }

    private void BeginProcessing()
    {
        _isProcessing = true;
        OnStateChanged?.Invoke();

        // When the user responds to clarification questions, reset back to Researching
        // so search_products becomes available again for the LLM to complete the plan.
        if (conversationManager.Phase == WorkflowPhase.AwaitingClarification)
        {
            conversationManager.ResetWorkflow();
        }
    }

    private async Task<ResilientChatClient> CreateResilientClientAsync()
    {
        var primaryClient = await chatClientProvider.GetChatClientAsync();
        var fallbackClient = agentOptions.Value.ModelFallbackEnabled
            ? await chatClientProvider.GetFallbackChatClientAsync()
            : null;

#pragma warning disable IDISP001 // Dispose created
        return new ResilientChatClient(
            primaryClient,
            fallbackClient,
            resilientLogger,
            llmOptions.Value,
            agentOptions.Value,
            metrics,
            retryPolicyFactory);
#pragma warning restore IDISP001
    }
}
