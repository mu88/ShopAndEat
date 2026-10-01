#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
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
public class ToolExecutionOrchestratorTests
{
    private readonly List<Activity> _completedActivities = [];
    private readonly List<(string Name, int Value, KeyValuePair<string, object?>[] Tags)> _intMeasurements = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _doubleMeasurements = [];
    private IToolCallDispatcher _dispatcherMock = null!;
    private IToolResultRenderer _rendererMock = null!;
    private IToolResultCompressor _compressorMock = null!;
    private IStringLocalizer<Messages> _localizerMock = null!;
    private ShoppingAgentMetrics _metrics = null!;
    private IOptions<AgentOptions> _agentOptions = null!;
    private ToolExecutionOrchestrator _sut = null!;
    private ActivityListener _activityListener = null!;
    private MeterListener _meterListener = null!;

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

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        _metrics = new ShoppingAgentMetrics(meterFactory);

        _agentOptions = Options.Create(new AgentOptions());
        _sut = new ToolExecutionOrchestrator(
            _dispatcherMock,
            _rendererMock,
            _compressorMock,
            _localizerMock,
            NullLogger<ToolExecutionOrchestrator>.Instance,
            _metrics,
            _agentOptions);

        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ShoppingAgentDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);

        _intMeasurements.Clear();
        _doubleMeasurements.Clear();
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, ShoppingAgentDiagnostics.MeterName, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
            _intMeasurements.Add((instrument.Name, measurement, tags.ToArray())));
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            _doubleMeasurements.Add((instrument.Name, measurement, tags.ToArray())));
        _meterListener.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    [Test]
    public async Task ProcessResponseAsync_SimpleTextResponse_StreamsText()
    {
        // Arrange
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello from the agent")]);
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("Hello from the agent");
        history.Should().HaveCount(2);
        history[1].Role.Should().Be(ChatRole.Assistant);
        state.ShouldBreak.Should().BeTrue();
    }

    [Test]
    public async Task ProcessResponseAsync_ExecutesToolCalls_AndReturnsRenderedResults()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?> { ["search_term"] = "Tofu" });
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("2 products found", true));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<group>");
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<tool>");
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns("<result>");
        _rendererMock.RenderToolGroupEnd().Returns("</group>");

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain("<group>");
        results.Should().Contain("<tool>");
        results.Should().Contain("<result>");
        results.Should().Contain("</group>");
        history.Should().HaveCount(3);
        history[1].Role.Should().Be(ChatRole.Assistant);
        history[2].Role.Should().Be(ChatRole.Tool);
        state.ShouldBreak.Should().BeFalse();
    }

    [Test]
    public async Task ProcessResponseAsync_StopsAfterRepeatedToolFailure()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 2, MaxToolCallingIterations = 10 });
        _sut = new ToolExecutionOrchestrator(
            _dispatcherMock,
            _rendererMock,
            _compressorMock,
            _localizerMock,
            NullLogger<ToolExecutionOrchestrator>.Instance,
            _metrics,
            _agentOptions);

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?> { ["search_term"] = "Tofu" });
        var toolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("Search failed", false));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();
        var results = new List<string>();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(toolResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        await foreach (var chunk in _sut.ProcessResponseAsync(toolResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain(result => result.Contains("RepeatedToolFailure"));
        state.ShouldBreak.Should().BeTrue();
    }

    [Test]
    public async Task ProcessResponseAsync_DoesNotQueryBreakFlag_WhenRepeatedFailureAlreadyStoppedProcessing()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 1 });
        _sut = CreateTestee();

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(_ => throw new AssertionException("ShouldBreakAfterToolExecution must not be queried after yield break"));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        state.ShouldBreak.Should().BeTrue();
    }

    [Test]
    public async Task ProcessResponseAsync_ExecutesToolCalls_WhenNoActivityListenerIsRegistered()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `groupActivity?.SetTag` branches used for optional OpenTelemetry tagging.
        _activityListener.Dispose();
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?> { ["search_term"] = "Tofu" });
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("2 products found", true));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<group>");
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<tool>");
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns("<result>");
        _rendererMock.RenderToolGroupEnd().Returns("</group>");

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain("<result>");
    }

    [Test]
    public async Task ProcessResponseAsync_StopsAfterRepeatedToolFailure_WhenNoActivityListenerIsRegistered()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `groupActivity?.SetStatus` branch on the repeated-failure path.
        _activityListener.Dispose();
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 2, MaxToolCallingIterations = 10 });
        _sut = new ToolExecutionOrchestrator(
            _dispatcherMock,
            _rendererMock,
            _compressorMock,
            _localizerMock,
            NullLogger<ToolExecutionOrchestrator>.Instance,
            _metrics,
            _agentOptions);

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?> { ["search_term"] = "Tofu" });
        var toolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("search_term=Tofu");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("Search failed", false));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();
        var results = new List<string>();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(toolResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        await foreach (var chunk in _sut.ProcessResponseAsync(toolResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        state.ShouldBreak.Should().BeTrue();
    }

    [Test]
    public async Task ProcessResponseAsync_BreaksLoop_WhenDispatcherSignalsShouldBreak()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent("call-1", "confirm_cart", new Dictionary<string, object?>());
        var toolResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("workflow", "Shopping Plan", "📋", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("__phase:awaiting_confirmation__", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "proceed") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(toolResponse, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        state.ShouldBreak.Should().BeTrue();
        await _dispatcherMock.Received(1).DispatchAsync(toolCallContent, "coop", Arg.Any<CancellationToken>());
        _rendererMock.Received(1).RenderToolCallStart("confirm_cart", Arg.Any<string>());
        _rendererMock.Received(1).RenderToolResult("confirm_cart", "__phase:awaiting_confirmation__");
    }

    [Test]
    public async Task ProcessResponseAsync_WhenResponseContainsTextAndToolCall_YieldsTextBeforeToolGroup()
    {
        // Arrange
        const string planTableText = "Here is your shopping plan:\n| Product | Qty |\n|---------|-----|\n| Tomatoes | 1 |";
        var toolCallContent = new FunctionCallContent("call-1", "confirm_cart", new Dictionary<string, object?>());
        var mixedContents = new List<AIContent>
        {
            new TextContent(planTableText),
            toolCallContent,
        };
        var mixedResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, mixedContents)]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("workflow", "Shopping Plan", "📋", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("__phase:awaiting_confirmation__", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns("<group>");
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns("</group>");

        var history = new List<ChatMessage> { new(ChatRole.User, "Here is my shopping list") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(mixedResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain(planTableText);
        results.Should().Contain("<group>");
        results.Should().Contain("</group>");
        results.IndexOf(planTableText).Should().BeLessThan(results.IndexOf("<group>"));
        _rendererMock.Received(1).RenderToolCallStart("confirm_cart", Arg.Any<string>());
        _rendererMock.Received(1).RenderToolResult("confirm_cart", "__phase:awaiting_confirmation__");
    }

    [Test]
    public async Task ProcessResponseAsync_WhenRequestClarificationCalledSilently_DoesNotBreakLoop()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent(
            "call-1",
            "request_clarification",
            new Dictionary<string, object?> { ["pending_items"] = "Garlic, Muesli" });
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("clarification", "Clarification Needed", "❓", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("pending_items=Garlic, Muesli");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("AWAITING CLARIFICATION.", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);
        _dispatcherMock.Phase.Returns(WorkflowPhase.AwaitingClarification);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Naturaplan apples, remove sesame") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        state.ShouldBreak.Should().BeFalse();
        _rendererMock.Received(1).RenderToolCallStart("request_clarification", "pending_items=Garlic, Muesli");
    }

    [Test]
    public async Task ProcessResponseAsync_WhenRequestClarificationCalledWithInlineText_BreaksLoopImmediately()
    {
        // Arrange
        const string planText = "Here is your plan:\n| Garlic | ❓ |\nPlease choose a product for Garlic.";
        var toolCallContent = new FunctionCallContent(
            "call-1",
            "request_clarification",
            new Dictionary<string, object?> { ["pending_items"] = "Garlic" });

        var mixedContents = new List<AIContent> { new TextContent(planText), toolCallContent };
        var mixedResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, mixedContents)]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("clarification", "Clarification Needed", "❓", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns("pending_items=Garlic");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("AWAITING CLARIFICATION.", true));
        _dispatcherMock.ShouldBreakAfterToolExecution.Returns(true);
        _dispatcherMock.Phase.Returns(WorkflowPhase.AwaitingClarification);

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Here is my shopping list") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(mixedResponse, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain(planText);
        state.ShouldBreak.Should().BeTrue();
        _rendererMock.Received(1).RenderToolCallStart("request_clarification", "pending_items=Garlic");
    }

    [Test]
    public async Task ProcessResponseAsync_JoinsMultipleTextContents_WithoutSeparator()
    {
        // Arrange
        var mixedContents = new List<AIContent> { new TextContent("Hello"), new TextContent("World") };
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, mixedContents)]);
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var state = new ConversationProcessingState();

        // Act
        var results = new List<string>();
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("HelloWorld");
    }

    [Test]
    public async Task ProcessResponseAsync_RecordsActivityTags_ForToolGroup()
    {
        // Arrange
        var toolCallContent1 = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var toolCallContent2 = new FunctionCallContent("call-2", "add_to_cart", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent1, toolCallContent2])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent1, toolCallContent2])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("ok", true));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.ToolExecution");
        activity.GetTagItem("tool.group").Should().Be("search");
        activity.GetTagItem("tool.count").Should().Be(2);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task ProcessResponseAsync_SetsErrorStatusOnGroupActivity_WhenRepeatedFailureOccurs()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 1 });
        _sut = CreateTestee();

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Contain("search_products");
    }

    [Test]
    public async Task ProcessResponseAsync_Throws_WhenCancellationTokenIsAlreadyCancelled()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () =>
        {
            await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state, cts.Token))
            {
                _ = chunk;
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task ProcessResponseAsync_RecordsSuccessMetrics_ButNotFailureMetrics_WhenToolSucceeds()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("ok", true));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        _intMeasurements.Should().ContainSingle(measurement =>
            measurement.Name == "shopping_agent.tool_calls.total"
            && measurement.Tags.Any(tag => tag.Key == "tool.name" && Equals(tag.Value, "search_products")));
        _intMeasurements.Should().NotContain(measurement => measurement.Name == "shopping_agent.tool_calls.failed");
        _doubleMeasurements.Should().ContainSingle(measurement =>
            measurement.Name == "shopping_agent.tool.execution_time_ms"
            && measurement.Tags.Any(tag => tag.Key == "tool.name" && Equals(tag.Value, "search_products")));
    }

    [Test]
    public async Task ProcessResponseAsync_RecordsFailureMetric_WhenToolFails()
    {
        // Arrange
        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        _intMeasurements.Should().ContainSingle(measurement =>
            measurement.Name == "shopping_agent.tool_calls.failed"
            && measurement.Tags.Any(tag => tag.Key == "tool.name" && Equals(tag.Value, "search_products")));
    }

    [Test]
    public async Task ProcessResponseAsync_StopsProcessingGroup_WhenFirstToolInGroupTriggersRepeatedFailure()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 1 });
        _sut = CreateTestee();

        var toolCallContent1 = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var toolCallContent2 = new FunctionCallContent("call-2", "add_to_cart", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent1, toolCallContent2])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent1, toolCallContent2])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        await _dispatcherMock.Received(1).DispatchAsync(toolCallContent1, "coop", Arg.Any<CancellationToken>());
        await _dispatcherMock.DidNotReceive().DispatchAsync(toolCallContent2, "coop", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessResponseAsync_StopsProcessingFurtherGroups_WhenFirstGroupTriggersRepeatedFailure()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 1 });
        _sut = CreateTestee();

        var toolCallContent1 = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var toolCallContent2 = new FunctionCallContent("call-2", "confirm_cart", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent1, toolCallContent2])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns(
            [
                ("search", "Searching", "🔍", [toolCallContent1]),
                ("workflow", "Shopping Plan", "📋", [toolCallContent2]),
            ]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));

        _rendererMock.RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolCallStart(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolResult(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        _rendererMock.RenderToolGroupEnd().Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act
        await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        _rendererMock.Received(1).RenderToolGroupStart(Arg.Any<string>(), Arg.Any<string>());
        await _dispatcherMock.DidNotReceive().DispatchAsync(toolCallContent2, "coop", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessResponseAsync_UsesSeparatorInFailureKey_ToAvoidNameArgumentCollisions()
    {
        // Arrange: "ab" + "c" without a separator equals "a" + "bc" without a separator ("abc" == "abc"),
        // but with the "|" separator the keys "ab|c" and "a|bc" are distinct.
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 2 });
        _sut = CreateTestee();

        var toolCallA = new FunctionCallContent("call-1", "ab", new Dictionary<string, object?> { ["x"] = "c" });
        var toolCallB = new FunctionCallContent("call-2", "a", new Dictionary<string, object?> { ["x"] = "bc" });

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns(callInfo => [("group", "Group", "🔍", (IReadOnlyList<FunctionCallContent>)callInfo.Arg<List<FunctionCallContent>>())]);
        _dispatcherMock.FormatArgs(Arg.Is<IDictionary<string, object?>>(args => Equals(args["x"], "c"))).Returns("c");
        _dispatcherMock.FormatArgs(Arg.Is<IDictionary<string, object?>>(args => Equals(args["x"], "bc"))).Returns("bc");
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false));

        var history = new List<ChatMessage> { new(ChatRole.User, "Search") };
        var state = new ConversationProcessingState();

        // Act: each colliding key fails exactly once -> with the separator neither reaches the threshold of 2
        var responseA = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallA])]);
        var responseB = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallB])]);
        await foreach (var chunk in _sut.ProcessResponseAsync(responseA, history, "coop", state))
        {
            _ = chunk;
        }

        await foreach (var chunk in _sut.ProcessResponseAsync(responseB, history, "coop", state))
        {
            _ = chunk;
        }

        // Assert
        state.RepeatedFailureTool.Should().BeNull();
    }

    [Test]
    public async Task ProcessResponseAsync_ResetsFailureCount_WhenToolSucceedsBeforeReachingThreshold()
    {
        // Arrange
        _agentOptions = Options.Create(new AgentOptions { ToolFailureThreshold = 2 });
        _sut = CreateTestee();

        var toolCallContent = new FunctionCallContent("call-1", "search_products", new Dictionary<string, object?>());
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, [toolCallContent])]);

        _dispatcherMock.GroupConsecutiveToolCalls(Arg.Any<List<FunctionCallContent>>())
            .Returns([("search", "Searching", "🔍", [toolCallContent])]);
        _dispatcherMock.FormatArgs(Arg.Any<IDictionary<string, object?>>()).Returns(string.Empty);

        var history = new List<ChatMessage> { new(ChatRole.User, "Search Tofu") };
        var state = new ConversationProcessingState();

        // Act: fail once, then succeed (resetting the counter), then fail once more -> must not reach the threshold of 2
        _dispatcherMock.DispatchAsync(Arg.Any<FunctionCallContent>(), "coop", Arg.Any<CancellationToken>())
            .Returns(("failed", false), ("ok", true), ("failed", false));

        for (var i = 0; i < 3; i++)
        {
            await foreach (var chunk in _sut.ProcessResponseAsync(response, history, "coop", state))
            {
                _ = chunk;
            }
        }

        // Assert
        state.RepeatedFailureTool.Should().BeNull();
    }

    private ToolExecutionOrchestrator CreateTestee() =>
        new(
            _dispatcherMock,
            _rendererMock,
            _compressorMock,
            _localizerMock,
            NullLogger<ToolExecutionOrchestrator>.Instance,
            _metrics,
            _agentOptions);
}
