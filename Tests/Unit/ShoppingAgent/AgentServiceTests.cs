using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ModelChatMessage = ShoppingAgent.Models.ChatMessage;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class AgentServiceTests
{
    [Test]
    public async Task ProcessMessageAsync_ReturnsLlmResponse_WhenNoToolCalls()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(chatClientMock);

        var response = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Hello! How can I help?")]);
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("Hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        string.Join(string.Empty, chunks).Should().Contain("Hello! How can I help?");
    }

    [Test]
    public async Task ProcessMessageAsync_ReturnsError_WhenLlmThrows()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(chatClientMock);

        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(x => throw new HttpRequestException("Connection refused"));

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            chunks.Add(chunk);
        }

        // Assert
        var result = string.Join(string.Empty, chunks);
        result.Should().Contain("Connection refused");
    }

    [Test]
    public async Task ProcessMessageAsync_ExecutesToolCall_AndReturnsResult()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var toolExecutorMock = Substitute.For<IShopToolExecutor>();
        var testee = CreateTestee(chatClientMock, toolExecutorMock);

        var searchProducts = new List<ShopProduct>
        {
            new() { Name = "Organic Tofu", Price = "2.95", Url = "https://coop.ch/p/123" },
        };
        toolExecutorMock.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(searchProducts);

        // First call: return tool call
        var toolCallContent = new FunctionCallContent(
            "call_1",
            "search_products",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal) { ["search_term"] = "Tofu" });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var firstResponse = new ChatResponse([assistantMessage]);

        // Second call: return final text
        var finalResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "I found Organic Tofu.")]);

        var callCount = 0;
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? firstResponse : finalResponse);
            });

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("Search Tofu"))
        {
            chunks.Add(chunk);
        }

        // Assert
        var fullOutput = string.Join(string.Empty, chunks);
        fullOutput.Should().Contain("search_products");
        fullOutput.Should().Contain("Organic Tofu");
        await toolExecutorMock.Received(1).SearchAsync("Tofu", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsProcessing_IsFalseAfterProcessing()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(chatClientMock);

        var response = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done")]);
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        testee.IsProcessing.Should().BeFalse();

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert
        testee.IsProcessing.Should().BeFalse();
    }

    [Test]
    public async Task ProcessMessageAsync_SavePreference_CallsPreferencesService()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var preferencesMock = Substitute.For<IPreferencesService>();
        preferencesMock.GetAllPreferencesAsync(Arg.Any<string>()).Returns(new List<PreferenceDto>());
        var testee = CreateTestee(chatClientMock, preferencesService: preferencesMock);

        var toolCallContent = new FunctionCallContent(
            "call_save",
            "save_preference",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal)
            {
                ["scope"] = "article:Tofu",
                ["key"] = "confirmed_product",
                ["value"] = "Organic Tofu, https://coop.ch/p/123",
            });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var firstResponse = new ChatResponse([assistantMessage]);
        var finalResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Preference saved.")]);

        var callCount = 0;
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? firstResponse : finalResponse));

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("Buy Tofu"))
        {
            _ = chunk;
        }

        // Assert
        await preferencesMock.Received(1).SavePreferenceAsync(
            Arg.Is<PreferenceDto>(p =>
                p.Scope == "article:Tofu" &&
                p.Key == "confirmed_product" &&
                p.Value == "Organic Tofu, https://coop.ch/p/123" &&
                p.StoreKey == "coop"));
    }

    [Test]
    public async Task ProcessMessageAsync_DeletePreference_CallsPreferencesService()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var preferencesMock = Substitute.For<IPreferencesService>();
        preferencesMock.GetAllPreferencesAsync(Arg.Any<string>()).Returns(new List<PreferenceDto>());
        preferencesMock.DeletePreferenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var testee = CreateTestee(chatClientMock, preferencesService: preferencesMock);

        var toolCallContent = new FunctionCallContent(
            "call_delete",
            "delete_preference",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal)
            {
                ["scope"] = "article:Tofu",
                ["key"] = "confirmed_product",
            });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var firstResponse = new ChatResponse([assistantMessage]);
        var finalResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Deleted.")]);

        var callCount = 0;
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? firstResponse : finalResponse));

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("Forget Tofu"))
        {
            _ = chunk;
        }

        // Assert
        await preferencesMock.Received(1).DeletePreferenceAsync("article:Tofu", "confirmed_product", "coop");
    }

    [Test]
    public async Task ProcessMessageAsync_AddToCart_CallsToolExecutorWithCorrectArgs()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var toolExecutorMock = Substitute.For<IShopToolExecutor>();
        toolExecutorMock.AddToCartAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("added:1");
        var testee = CreateTestee(chatClientMock, toolExecutorMock);

        var toolCallContent = new FunctionCallContent(
            "call_cart",
            "add_to_cart",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal)
            {
                ["product_url"] = "https://coop.ch/p/123",
                ["quantity"] = "2",
            });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var firstResponse = new ChatResponse([assistantMessage]);
        var finalResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done.")]);

        var callCount = 0;
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? firstResponse : finalResponse));

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("Buy Tofu"))
        {
            _ = chunk;
        }

        // Assert
        await toolExecutorMock.Received(1).AddToCartAsync("https://coop.ch/p/123", 2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessMessageAsync_AddToCart_UsesDefaultQuantityOne_WhenArgMissing()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var toolExecutorMock = Substitute.For<IShopToolExecutor>();
        toolExecutorMock.AddToCartAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("added:1");
        var testee = CreateTestee(chatClientMock, toolExecutorMock);

        // Tool call with no arguments – GetArg should return empty string, quantity defaults to 1
        var toolCallContent = new FunctionCallContent(
            "call_cart",
            "add_to_cart",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal));
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var firstResponse = new ChatResponse([assistantMessage]);
        var finalResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done.")]);

        var callCount = 0;
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? firstResponse : finalResponse));

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("Buy Tofu"))
        {
            _ = chunk;
        }

        // Assert
        await toolExecutorMock.Received(1).AddToCartAsync(string.Empty, 1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessMessageAsync_CompletesFinitely_WhenLlmAlwaysReturnsToolCalls()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var toolExecutorMock = Substitute.For<IShopToolExecutor>();
        toolExecutorMock.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ShopProduct>());
        var testee = CreateTestee(chatClientMock, toolExecutorMock);

        // LLM always returns a search_products tool call
        var toolCallContent = new FunctionCallContent(
            "call_search",
            "search_products",
            new Dictionary<string, object>(global::System.StringComparer.Ordinal) { ["search_term"] = "Tofu" });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var toolResponse = new ChatResponse([assistantMessage]);

        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(toolResponse));

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("Search Tofu"))
        {
            chunks.Add(chunk);
        }

        // Assert
        // The loop must have broken at max iterations (50); SearchAsync called at most 50 times
        await toolExecutorMock.Received(50).SearchAsync("Tofu", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessMessageAsync_ReturnsErrorMessage_WhenLlmThrowsOperationCanceledException()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(chatClientMock);

        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new OperationCanceledException("simulated timeout"));

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            chunks.Add(chunk);
        }

        // Assert
        var result = string.Join(string.Empty, chunks);
        result.Should().NotBeEmpty("an error message should be shown when the LLM call fails");
    }

    [Test]
    public async Task SwitchShopAsync_ClearsMessagesAndReinitializes()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        chatClientMock.GetResponseAsync(Arg.Any<IEnumerable<AiChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Hi")])));
        var testee = CreateTestee(chatClientMock);

        // Seed one message
        testee.Messages.Add(new ModelChatMessage { Role = "user", Content = "Hello" });
        testee.Messages.Should().HaveCount(1);

        // Act
        await testee.SwitchShopAsync("coop");

        // Assert
        testee.Messages.Should().BeEmpty("SwitchShopAsync must clear the conversation");
        testee.SelectedShopKey.Should().Be("coop");
    }

    [Test]
    public async Task InitializeAsync_DoesNotRebuildPrompt_WhenAlreadyInitializedWithSameShop()
    {
        // Arrange
        var promptBuilderMock = Substitute.For<ISystemPromptBuilder>();
        promptBuilderMock
            .BuildSystemPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("system prompt"));
        var testee = CreateTestee(Substitute.For<IChatClient>(), systemPromptBuilder: promptBuilderMock);

        await testee.InitializeAsync("coop");

        // Act — second call with the same shop key must hit the early return
        await testee.InitializeAsync("coop");

        // Assert — BuildSystemPromptAsync called only once despite two InitializeAsync calls
        await promptBuilderMock.Received(1).BuildSystemPromptAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InitializeAsync_AlwaysResetsWorkflowState()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        var promptBuilderMock = Substitute.For<ISystemPromptBuilder>();
        promptBuilderMock
            .BuildSystemPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("system prompt"));
        var testee = CreateTestee(
            Substitute.For<IChatClient>(),
            systemPromptBuilder: promptBuilderMock,
            conversationManager: conversationManagerMock);

        await testee.InitializeAsync("coop");

        // Act — second call with the same shop triggers the early-return path
        await testee.InitializeAsync("coop");

        // Assert — ResetWorkflow() called once per InitializeAsync invocation, even when early-return fires
        conversationManagerMock.Received(2).ResetWorkflow();
    }

    [Test]
    public async Task ProcessMessageAsync_PassesFuncToConversationManager_ThatReturnsPhaseBasedTools()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        conversationManagerMock.Phase.Returns(WorkflowPhase.Researching);

        var toolDefinitionProviderMock = Substitute.For<IToolDefinitionProvider>();
        toolDefinitionProviderMock
            .GetToolDefinitions(Arg.Any<string>(), Arg.Any<WorkflowPhase>())
            .Returns([]);

        Func<IReadOnlyList<AITool>> capturedGetTools = null;
        conversationManagerMock
            .ProcessAsync(
                Arg.Any<IList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Do<Func<IReadOnlyList<AITool>>>(func => capturedGetTools = func),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        var testee = CreateTestee(
            Substitute.For<IChatClient>(),
            conversationManager: conversationManagerMock,
            toolDefinitionProvider: toolDefinitionProviderMock);

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert
        conversationManagerMock.Received(1).ProcessAsync(
            Arg.Any<IList<AiChatMessage>>(),
            Arg.Any<IChatClient>(),
            Arg.Any<Func<IReadOnlyList<AITool>>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        capturedGetTools.Should().NotBeNull();
        capturedGetTools!();
        toolDefinitionProviderMock.Received(1).GetToolDefinitions("Coop", WorkflowPhase.Researching);
    }

    [Test]
    public async Task ProcessMessageAsync_ResetsWorkflow_WhenPhaseIsAwaitingClarification()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        conversationManagerMock.Phase.Returns(WorkflowPhase.AwaitingClarification);
        conversationManagerMock
            .ProcessAsync(
                Arg.Any<IList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        var testee = CreateTestee(
            Substitute.For<IChatClient>(),
            conversationManager: conversationManagerMock);

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("Garlic please"))
        {
            _ = chunk;
        }

        // Assert — ResetWorkflow called once by InitializeAsync, once more for the AwaitingClarification auto-reset
        conversationManagerMock.Received(2).ResetWorkflow();
    }

    [Test]
    public async Task ProcessMessageAsync_DoesNotResetWorkflow_WhenPhaseIsResearching()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        conversationManagerMock.Phase.Returns(WorkflowPhase.Researching);
        conversationManagerMock
            .ProcessAsync(
                Arg.Any<IList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        var testee = CreateTestee(
            Substitute.For<IChatClient>(),
            conversationManager: conversationManagerMock);

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert — ResetWorkflow called only once by InitializeAsync, NOT a second time for Researching phase
        conversationManagerMock.Received(1).ResetWorkflow();
    }

    private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static AgentService CreateTestee(
        IChatClient chatClient,
        IShopToolExecutor toolExecutor = null,
        IPreferencesService preferencesService = null,
        ISystemPromptBuilder systemPromptBuilder = null,
        IConversationManager conversationManager = null,
        IToolDefinitionProvider toolDefinitionProvider = null)
    {
        toolExecutor ??= Substitute.For<IShopToolExecutor>();

        var preferencesMock = preferencesService ?? Substitute.For<IPreferencesService>();
        if (preferencesService == null)
        {
            preferencesMock.GetAllPreferencesAsync(Arg.Any<string>()).Returns(new List<PreferenceDto>());
        }

        var chatClientProviderMock = Substitute.For<IMistralChatClientProvider>();
        chatClientProviderMock.GetChatClientAsync().Returns(Task.FromResult(chatClient));

        var localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
        {
            var key = call.ArgAt<string>(0);
            var args = call.ArgAt<object[]>(1);
            var formatted = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, "{0}", args);
            return new LocalizedString(key, $"{key}: {formatted}");
        });

        var sessionMock = Substitute.For<ISessionService>();
        sessionMock.GetUnitsAsync().Returns(new List<string>());

        var factoryMock = Substitute.For<IShopToolExecutorFactory>();
        factoryMock.AvailableShops.Returns(new List<ShopConfig> { new("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart") });
        factoryMock.GetExecutor("coop").Returns(toolExecutor);

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);

        var systemPromptBuilderInstance = systemPromptBuilder ?? new SystemPromptBuilder(preferencesMock, sessionMock, localizerMock);
        var toolDefinitionProviderInstance = toolDefinitionProvider ?? new ToolDefinitionProvider();
        var workflowStateMock = Substitute.For<IShoppingWorkflowState>();
        var toolCallDispatcher = new ToolCallDispatcher(factoryMock, preferencesMock, Substitute.For<IShoppingListVerifier>(), localizerMock, workflowStateMock);
        var conversationManagerInstance = conversationManager ?? new ConversationManager(toolCallDispatcher, new HtmlToolResultRenderer(localizerMock), new ToolResultCompressor(), localizerMock, NullLogger<ConversationManager>.Instance, metrics, Options.Create(new AgentOptions()), Options.Create(new LlmClientOptions()));
        var shopSessionManager = new ShopSessionManager(factoryMock, NullLogger<ShopSessionManager>.Instance);

        return new AgentService(chatClientProviderMock, systemPromptBuilderInstance, toolDefinitionProviderInstance, conversationManagerInstance, shopSessionManager, metrics, NullLogger<AgentService>.Instance);
    }
}
