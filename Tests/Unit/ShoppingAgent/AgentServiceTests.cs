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
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal) { ["search_term"] = "Tofu" });
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
    public async Task ProcessMessageAsync_RaisesOnStateChanged_AtStartAndEnd()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(chatClientMock);
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done")])));

        var processingStatesAtEachEvent = new List<bool>();
        testee.OnStateChanged += () => processingStatesAtEachEvent.Add(testee.IsProcessing);

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert — exactly two invocations: once when processing starts (IsProcessing=true),
        // once in the finally block when it ends (IsProcessing=false).
        processingStatesAtEachEvent.Should().Equal(true, false);
    }

    [Test]
    public async Task ProcessMessageAsync_IncrementsMessagesProcessedMetric()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done")])));
        var (testee, _, metrics) = CreateTesteeWithDependencies(chatClientMock);

        var intMeasurements = new List<int>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Name, metrics.MessagesProcessed.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<int>((_, measurement, _, _) => intMeasurements.Add(measurement));
        meterListener.Start();

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert
        intMeasurements.Should().ContainSingle().Which.Should().Be(1);
    }

    [Test]
    public async Task ProcessMessageAsync_CallsFallbackClient_WhenModelFallbackEnabled()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done")])));
        var (testee, chatClientProviderMock, _) = CreateTesteeWithDependencies(
            chatClientMock,
            agentOptionsOverride: new AgentOptions { ModelFallbackEnabled = true });

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert
        await chatClientProviderMock.Received(1).GetFallbackChatClientAsync();
    }

    [Test]
    public async Task ProcessMessageAsync_DoesNotCallFallbackClient_WhenModelFallbackDisabled()
    {
        // Arrange
        var chatClientMock = Substitute.For<IChatClient>();
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, "Done")])));
        var (testee, chatClientProviderMock, _) = CreateTesteeWithDependencies(
            chatClientMock,
            agentOptionsOverride: new AgentOptions { ModelFallbackEnabled = false });

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("test"))
        {
            _ = chunk;
        }

        // Assert
        await chatClientProviderMock.DidNotReceive().GetFallbackChatClientAsync();
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
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal)
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
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal)
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
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal)
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
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal));
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
        var testee = CreateTesteeWithDependencies(
            chatClientMock,
            toolExecutor: toolExecutorMock,
            agentOptionsOverride: new AgentOptions { MaxToolCallingIterations = 3 }).Testee;

        // LLM always returns a search_products tool call
        var toolCallContent = new FunctionCallContent(
            "call_search",
            "search_products",
            new Dictionary<string, object?>(global::System.StringComparer.Ordinal) { ["search_term"] = "Tofu" });
        var assistantMessage = new AiChatMessage(ChatRole.Assistant, new List<AIContent> { toolCallContent });
        var toolResponse = new ChatResponse([assistantMessage]);
        var llmCallCount = 0;

        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                llmCallCount++;
                if (llmCallCount > 3)
                {
                    throw new InvalidOperationException("Too many iterations");
                }

                return Task.FromResult(toolResponse);
            });

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in testee.ProcessMessageAsync("Search Tofu"))
        {
            chunks.Add(chunk);
        }

        // Assert
        // The loop must have broken at max iterations (3); with a decrement mutation, the fourth
        // LLM call would throw and fail the test instead of hanging until Stryker times out it.
        await toolExecutorMock.Received(3).SearchAsync("Tofu", Arg.Any<CancellationToken>());
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
    public async Task ProcessMessageAsync_WhenCancelledMidStream_StillAppendsMessagesProducedBeforeCancellation()
    {
        // Arrange — regression test: a cancellation/exception after ConversationManager already
        // produced messages (e.g. a tool call with real side effects) must not discard them; they
        // need to be appended to _conversationHistory so the next turn's LLM call sees them.
        var producedMessage = new AiChatMessage(ChatRole.Assistant, "Produced before cancellation");
        var newMessages = new List<AiChatMessage>();

        async IAsyncEnumerable<string> ThrowingChunksAsync()
        {
            yield return "partial";
            newMessages.Add(producedMessage);
            await Task.Yield();
            throw new OperationCanceledException("Cancelled mid-stream");
        }

        IReadOnlyList<AiChatMessage>? historyOnNextCall = null;
        var callCount = 0;
        var conversationManagerMock = Substitute.For<IConversationManager>();
        conversationManagerMock.Phase.Returns(WorkflowPhase.Researching);
        conversationManagerMock
            .ProcessAsync(
                Arg.Do<IReadOnlyList<AiChatMessage>>(history =>
                {
                    callCount++;
                    if (callCount == 2)
                    {
                        historyOnNextCall = history;
                    }
                }),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => CreateConversationProcessingResult(ThrowingChunksAsync(), newMessages),
                _ => CreateConversationProcessingResult());
        var testee = CreateTestee(Substitute.For<IChatClient>(), conversationManager: conversationManagerMock);

        // Act — first call is cancelled mid-stream; the second call is where we can observe
        // whether the first call's produced message survived into the conversation history.
        var act = async () =>
        {
            await foreach (var chunk in testee.ProcessMessageAsync("Hi"))
            {
                _ = chunk;
            }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();

        await foreach (var chunk in testee.ProcessMessageAsync("Still there?"))
        {
            _ = chunk;
        }

        // Assert
        historyOnNextCall.Should().NotBeNull();
        historyOnNextCall!.Should().Contain(producedMessage);
    }

    [Test]
    public void AddMessage_AppendsMessage_ToMessagesInOrder_AndKeepsListReadOnly()
    {
        // Arrange
        var testee = CreateTestee(Substitute.For<IChatClient>());
        var first = new ModelChatMessage { Role = "user", Content = "Hello" };
        var second = new ModelChatMessage { Role = "assistant", Content = "Hi there" };

        // Act
        testee.AddMessage(first);
        testee.AddMessage(second);

        // Assert — Messages is exposed as a read-only view; callers can no longer
        // mutate the underlying list except through the explicit AddMessage method.
        testee.Messages.Should().BeAssignableTo<IReadOnlyList<ModelChatMessage>>();
        testee.Messages.Should().ContainInOrder(first, second);
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
        testee.AddMessage(new ModelChatMessage { Role = "user", Content = "Hello" });
        testee.Messages.Should().HaveCount(1);

        // Act
        await testee.SwitchShopAsync("coop");

        // Assert
        testee.Messages.Should().BeEmpty("SwitchShopAsync must clear the conversation");
        testee.SelectedShopKey.Should().Be("coop");
    }

    [Test]
    public async Task SwitchShopAsync_ResetsShopSessionManager_SoPromptIsRebuiltEvenForTheSameShop()
    {
        // Arrange
        var promptBuilderMock = Substitute.For<ISystemPromptBuilder>();
        promptBuilderMock
            .BuildSystemPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("system prompt"));
        var testee = CreateTestee(Substitute.For<IChatClient>(), systemPromptBuilder: promptBuilderMock);
        await testee.InitializeAsync("coop");

        // Act — switching to the SAME shop key must still rebuild the prompt, because
        // SwitchShopAsync's shopSessionManager.Reset() must clear IsInitialized first.
        await testee.SwitchShopAsync("coop");

        // Assert
        await promptBuilderMock.Received(2).BuildSystemPromptAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
    public async Task InitializeAsync_ClearsConversationHistory_WhenReinitializingAfterReset()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        conversationManagerMock.Phase.Returns(WorkflowPhase.Researching);
        IReadOnlyList<AiChatMessage>? capturedHistory = null;
        conversationManagerMock
            .ProcessAsync(
                Arg.Do<IReadOnlyList<AiChatMessage>>(history => capturedHistory = history),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateConversationProcessingResult());
        var testee = CreateTestee(Substitute.For<IChatClient>(), conversationManager: conversationManagerMock);
        await testee.InitializeAsync("coop");

        // Act — SwitchShopAsync resets IsInitialized, forcing InitializeAsync to rebuild the prompt
        // via the Clear()+rebuild path (not the early-return path).
        await testee.SwitchShopAsync("coop");
        await foreach (var chunk in testee.ProcessMessageAsync("Hi"))
        {
            _ = chunk;
        }

        // Assert — only ONE System message must be present; without _conversationHistory.Clear(),
        // the first InitializeAsync's system prompt would still be there too, duplicated.
        capturedHistory.Should().NotBeNull();
        capturedHistory!.Count(message => message.Role == ChatRole.System).Should().Be(1);
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

        Func<IReadOnlyList<AITool>>? capturedGetTools = null;
        conversationManagerMock
            .ProcessAsync(
                Arg.Any<IReadOnlyList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Do<Func<IReadOnlyList<AITool>>>(func => capturedGetTools = func),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateConversationProcessingResult());

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
            Arg.Any<IReadOnlyList<AiChatMessage>>(),
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
                Arg.Any<IReadOnlyList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateConversationProcessingResult());

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
                Arg.Any<IReadOnlyList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateConversationProcessingResult());

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

    [Test]
    public async Task ProcessMessageAsync_AppendsReturnedMessagesToConversationHistory()
    {
        // Arrange
        var conversationManagerMock = Substitute.For<IConversationManager>();
        IReadOnlyList<AiChatMessage>? secondHistory = null;

        conversationManagerMock
            .ProcessAsync(
                Arg.Any<IReadOnlyList<AiChatMessage>>(),
                Arg.Any<IChatClient>(),
                Arg.Any<Func<IReadOnlyList<AITool>>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => CreateConversationProcessingResult(
                    newMessages:
                    [
                        new AiChatMessage(ChatRole.Assistant, "Assistant reply"),
                    ]),
                callInfo =>
                {
                    secondHistory = callInfo.ArgAt<IReadOnlyList<AiChatMessage>>(0);
                    return CreateConversationProcessingResult();
                });

        var testee = CreateTestee(
            Substitute.For<IChatClient>(),
            conversationManager: conversationManagerMock);

        // Act
        await foreach (var chunk in testee.ProcessMessageAsync("First"))
        {
            _ = chunk;
        }

        await foreach (var chunk in testee.ProcessMessageAsync("Second"))
        {
            _ = chunk;
        }

        // Assert
        secondHistory.Should().NotBeNull();
        secondHistory!.Select(message => message.Role).Should().ContainInOrder(ChatRole.System, ChatRole.User, ChatRole.Assistant, ChatRole.User);
        secondHistory[2].Text.Should().Be("Assistant reply");
    }

    private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static ConversationProcessingResult CreateConversationProcessingResult(
        IAsyncEnumerable<string>? chunks = null,
        IReadOnlyList<AiChatMessage>? newMessages = null)
        => new(
            chunks ?? EmptyAsyncEnumerable(),
            newMessages ?? []);

    private static AgentService CreateTestee(
        IChatClient chatClient,
        IShopToolExecutor? toolExecutor = null,
        IPreferencesService? preferencesService = null,
        ISystemPromptBuilder? systemPromptBuilder = null,
        IConversationManager? conversationManager = null,
        IToolDefinitionProvider? toolDefinitionProvider = null)
        => CreateTesteeWithDependencies(
            chatClient,
            toolExecutor,
            preferencesService,
            systemPromptBuilder,
            conversationManager,
            toolDefinitionProvider).Testee;

    private static (AgentService Testee, IMistralChatClientProvider ChatClientProviderMock, ShoppingAgentMetrics Metrics) CreateTesteeWithDependencies(
        IChatClient chatClient,
        IShopToolExecutor? toolExecutor = null,
        IPreferencesService? preferencesService = null,
        ISystemPromptBuilder? systemPromptBuilder = null,
        IConversationManager? conversationManager = null,
        IToolDefinitionProvider? toolDefinitionProvider = null,
        AgentOptions? agentOptionsOverride = null)
    {
        toolExecutor ??= Substitute.For<IShopToolExecutor>();

        var preferencesMock = preferencesService ?? Substitute.For<IPreferencesService>();
        if (preferencesService == null)
        {
            preferencesMock.GetAllPreferencesAsync(Arg.Any<string>()).Returns(new List<PreferenceDto>());
        }

        var chatClientProviderMock = Substitute.For<IMistralChatClientProvider>();
        chatClientProviderMock.GetChatClientAsync().Returns(Task.FromResult(chatClient));
        chatClientProviderMock.GetFallbackChatClientAsync().Returns(Task.FromResult(chatClient));

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

        var llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key" });
        var agentOptions = Options.Create(agentOptionsOverride ?? new AgentOptions());

        var systemPromptBuilderInstance = systemPromptBuilder ?? new SystemPromptBuilder(preferencesMock, sessionMock, localizerMock);
        var toolDefinitionProviderInstance = toolDefinitionProvider ?? new ToolDefinitionProvider();
        var workflowStateMock = Substitute.For<IShoppingWorkflowState>();
        var toolCallDispatcher = new ToolCallDispatcher(factoryMock, preferencesMock, Substitute.For<IShoppingListVerifier>(), localizerMock, workflowStateMock);
        var conversationManagerInstance = conversationManager ?? CreateConversationManager(toolCallDispatcher, localizerMock, metrics, agentOptions, llmOptions);
        var shopSessionManager = new ShopSessionManager(factoryMock, NullLogger<ShopSessionManager>.Instance);
        var retryPolicyFactory = new LlmRetryPolicyFactory(llmOptions, metrics, NullLogger<LlmRetryPolicyFactory>.Instance);

        var testee = new AgentService(
            chatClientProviderMock,
            systemPromptBuilderInstance,
            toolDefinitionProviderInstance,
            conversationManagerInstance,
            shopSessionManager,
            retryPolicyFactory,
            llmOptions,
            agentOptions,
            metrics,
            NullLogger<AgentService>.Instance,
            NullLogger<ResilientChatClient>.Instance);

        return (testee, chatClientProviderMock, metrics);
    }

    private static ConversationManager CreateConversationManager(
        ToolCallDispatcher toolCallDispatcher,
        IStringLocalizer<Messages> localizerMock,
        ShoppingAgentMetrics metrics,
        IOptions<AgentOptions> agentOptions,
        IOptions<LlmClientOptions> llmOptions)
        => new(
            toolCallDispatcher,
            new LlmCommunicator(localizerMock, NullLogger<LlmCommunicator>.Instance, metrics, llmOptions),
            new ToolExecutionOrchestrator(
                toolCallDispatcher,
                new HtmlToolResultRenderer(localizerMock),
                new ToolResultCompressor(),
                localizerMock,
                NullLogger<ToolExecutionOrchestrator>.Instance,
                metrics,
                agentOptions),
            NullLogger<ConversationManager>.Instance,
            metrics,
            agentOptions,
            llmOptions);
}
