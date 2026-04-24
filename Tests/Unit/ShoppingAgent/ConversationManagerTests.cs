#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ConversationManagerTests
{
    private IToolCallDispatcher _dispatcherMock;
    private IToolResultRenderer _rendererMock;
    private IToolResultCompressor _compressorMock;
    private IStringLocalizer<Messages> _localizerMock;
    private ILogger<ConversationManager> _loggerMock;
    private ShoppingAgentMetrics _metrics;
    private IOptions<AgentOptions> _agentOptions;
    private IOptions<LlmClientOptions> _llmOptions;
    private ConversationManager _sut;

    [SetUp]
    public void SetUp()
    {
        _dispatcherMock = Substitute.For<IToolCallDispatcher>();
        _rendererMock = Substitute.For<IToolResultRenderer>();
        _compressorMock = Substitute.For<IToolResultCompressor>();
        _compressorMock.Compress(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<string>(1));

        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        _localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));

        _loggerMock = NullLogger<ConversationManager>.Instance;

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        _metrics = new ShoppingAgentMetrics(meterFactory);

        _agentOptions = Options.Create(new AgentOptions());
        _llmOptions = Options.Create(new LlmClientOptions());

        _sut = new ConversationManager(
            _dispatcherMock,
            _rendererMock,
            _compressorMock,
            _localizerMock,
            _loggerMock,
            _metrics,
            _agentOptions,
            _llmOptions);
    }

    [Test]
    public async Task ProcessAsync_SimpleTextResponse_StreamsText()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello from the agent")]);
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool>();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("Hello from the agent");
        history.Should().HaveCount(2);
        history[1].Role.Should().Be(ChatRole.Assistant);
    }

    [Test]
    public async Task ProcessAsync_WhenToolCallingDisabled_FallsBackWithoutTools()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();

        // First call: throw tool-not-supported error
        // Second call (fallback without tools): return text
        var textResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fallback response")]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("Tool calling is NOT SUPPORTED by this model"),
                _ => Task.FromResult(textResponse));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain(s => s.Contains("Fallback response"));
    }

    [Test]
    public async Task ProcessAsync_WhenToolCallingDisabled_SubsequentCallOmitsTools()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();

        // First call: throw tool-not-supported error, triggering fallback
        var textResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fallback response")]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("Tool calling is NOT SUPPORTED by this model"),
                _ => Task.FromResult(textResponse));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Trigger fallback
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            _ = chunk;
        }

        // Arrange subsequent call
        history.Clear();
        history.Add(new ChatMessage(ChatRole.User, "Hello again"));
        chatClient.ClearReceivedCalls();

        var thirdResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "No tools here")]);
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(thirdResponse));

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("No tools here");
        await chatClient.Received().GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Is<ChatOptions>(o => o.Tools == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ReturnsTimeoutMessage_WhenLlmTimesOut()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        _llmOptions = Options.Create(new LlmClientOptions { TimeoutSeconds = 1 });
        _sut = new ConversationManager(_dispatcherMock, _rendererMock, _compressorMock, _localizerMock, _loggerMock, _metrics, _agentOptions, _llmOptions);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(2);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, "should not reach")]);
            });

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool>();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Contain("LlmTimeout");
    }

    [Test]
    public async Task ProcessAsync_ReturnsErrorMessage_WhenLlmThrowsGenericException()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new InvalidOperationException("Model overloaded"));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool>();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Contain("LlmError");
    }

    [Test]
    public async Task ProcessAsync_ExecutesToolCalls_AndReturnsRenderedResults()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object> { ["search_term"] = "Tofu" });
        var toolCallContents = new List<AIContent> { toolCallContent };
        var firstResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, toolCallContents)]);
        var secondResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Found 2 products for Tofu")]);

        var callCount = 0;
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? firstResponse : secondResponse);
            });

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("2 products found", true));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<group>");
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<tool>");
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns("<result>");
        _rendererMock.RenderToolGroupEnd().Returns("</group>");

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var tools = new List<AITool>();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain("<group>");
        results.Should().Contain("<tool>");
        results.Should().Contain("<result>");
        results.Should().Contain("</group>");
        results.Should().Contain("Found 2 products for Tofu");
    }

    [Test]
    public async Task ProcessAsync_StopsAfterRepeatedToolFailure()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 2, MaxToolCallingIterations = 10 });
        _sut = new ConversationManager(_dispatcherMock, _rendererMock, _compressorMock, _localizerMock, _loggerMock, _metrics, _agentOptions, _llmOptions);

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object> { ["search_term"] = "Tofu" });
        var toolCallContents = new List<AIContent> { toolCallContent };
        var toolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, toolCallContents)]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(toolResponse));

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("Search failed", false));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var tools = new List<AITool>();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain(s => s.Contains("RepeatedToolFailure"));
    }

    [Test]
    public async Task ProcessAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool>();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () =>
        {
            await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop", cts.Token))
            {
                _ = chunk;
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task ProcessAsync_WithTokenCancelledDuringToolExecution_ThrowsOperationCanceledException()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        using var cts = new CancellationTokenSource();

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object> { ["search_term"] = "Tofu" });
        var firstResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(firstResponse));

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                cts.Cancel();
                return ("results", true);
            });

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var tools = new List<AITool>();

        // Act
        var act = async () =>
        {
            await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop", cts.Token))
            {
                _ = chunk;
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void Phase_DelegatesToDispatcher()
    {
        // Arrange
        _dispatcherMock.Phase.Returns(WorkflowPhase.FillingCart);

        // Act
        var phase = _sut.Phase;

        // Assert
        phase.Should().Be(WorkflowPhase.FillingCart);
    }

    [Test]
    public void ResetWorkflow_DelegatesToDispatcher()
    {
        // Arrange & Act
        _sut.ResetWorkflow();

        // Assert
        _dispatcherMock.Received(1).ResetWorkflow();
    }

    [Test]
    public async Task ProcessAsync_BreaksLoop_WhenDispatcherSignalsShouldBreak()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var toolCallContent = new FunctionCallContent("call-1", "confirm_cart", new Dictionary<string, object>());
        var toolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(toolResponse));

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("workflow", "Shopping Plan", "📋", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("__phase:awaiting_confirmation__", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        List<ChatMessage> history = [new(ChatRole.User, "proceed")];
        List<AITool> tools = [];

        // Act
        List<string> results = [];
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert — loop breaks after first tool call; LLM was called exactly once
        await chatClient.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_WhenResponseContainsTextAndToolCall_YieldsTextBeforeToolGroup()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        const string planTableText = "Here is your shopping plan:\n| Product | Qty |\n|---------|-----|\n| Tomatoes | 1 |";
        var toolCallContent = new FunctionCallContent("call-1", "confirm_cart", new Dictionary<string, object>());

        // LLM returns text AND a tool call in the same response (typical when presenting plan + signalling confirm)
        var mixedContents = new List<AIContent>
        {
            new TextContent(planTableText),
            toolCallContent,
        };
        var mixedResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, mixedContents)]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mixedResponse));

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("workflow", "Shopping Plan", "📋", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("__phase:awaiting_confirmation__", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<group>");
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns("</group>");

        List<ChatMessage> history = [new(ChatRole.User, "Here is my shopping list")];
        List<AITool> tools = [];

        // Act
        List<string> results = [];
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert — plan table text appears in output AND the tool group is rendered
        results.Should().Contain(planTableText);
        results.Should().Contain("<group>");
        results.Should().Contain("</group>");

        // Text must appear BEFORE the tool group
        var textIndex = results.IndexOf(planTableText);
        var groupIndex = results.IndexOf("<group>");
        textIndex.Should().BeLessThan(groupIndex);
    }

    [Test]
    public async Task ProcessAsync_WhenRequestClarificationCalledSilently_ContinuesLoopToAllowTextGeneration()
    {
        // Arrange — simulates: LLM searches, then calls request_clarification with NO inline text.
        // The loop must NOT break so the LLM gets one more turn (with AwaitingClarification tools)
        // to produce the updated plan table as text.
        var chatClient = Substitute.For<IChatClient>();
        var toolCallContent = new FunctionCallContent(
            "call-1",
            "request_clarification",
            new Dictionary<string, object> { ["pending_items"] = "Garlic, Muesli" });

        var silentToolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);
        var textResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Updated plan: | Garlic | ❓ |")]);

        var callCount = 0;
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? silentToolResponse : textResponse);
            });

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("clarification", "Clarification Needed", "❓", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns("pending_items=Garlic, Muesli");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("AWAITING CLARIFICATION.", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);
        _dispatcherMock.Phase.Returns(WorkflowPhase.AwaitingClarification);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        List<ChatMessage> history = [new(ChatRole.User, "Naturaplan apples, remove sesame")];
        List<AITool> tools = [];

        // Act
        List<string> results = [];
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert — LLM must have been called twice: once for the silent tool call, once for the text response
        await chatClient.Received(2).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        results.Should().Contain(s => s.Contains("Updated plan"));
    }

    [Test]
    public async Task ProcessAsync_WhenRequestClarificationCalledWithInlineText_BreaksLoopImmediately()
    {
        // Arrange — simulates: LLM outputs the plan table as text AND calls request_clarification in the same response.
        // The loop MUST break immediately because the user has already seen the text.
        var chatClient = Substitute.For<IChatClient>();
        const string planText = "Here is your plan:\n| Garlic | ❓ |\nPlease choose a product for Garlic.";
        var toolCallContent = new FunctionCallContent(
            "call-1",
            "request_clarification",
            new Dictionary<string, object> { ["pending_items"] = "Garlic" });

        var mixedContents = new List<AIContent> { new TextContent(planText), toolCallContent };
        var mixedResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, mixedContents)]);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mixedResponse));

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("clarification", "Clarification Needed", "❓", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object>>()).Returns("pending_items=Garlic");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("AWAITING CLARIFICATION.", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);
        _dispatcherMock.Phase.Returns(WorkflowPhase.AwaitingClarification);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        List<ChatMessage> history = [new(ChatRole.User, "Here is my shopping list")];
        List<AITool> tools = [];

        // Act
        List<string> results = [];
        await foreach (var chunk in _sut.ProcessAsync(history, chatClient, () => tools, "coop"))
        {
            results.Add(chunk);
        }

        // Assert — LLM called exactly once; loop broke because text was already shown
        await chatClient.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        results.Should().Contain(planText);
    }
}
