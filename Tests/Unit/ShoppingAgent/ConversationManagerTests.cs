#pragma warning disable SA1011 // Closing square bracket should be followed by a space

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ConversationManagerTests
{
    private readonly List<Activity> _completedActivities = [];
    private IToolCallDispatcher _dispatcherMock = null!;
    private ILlmCommunicator _llmCommunicatorMock = null!;
    private IToolExecutionOrchestrator _toolExecutionOrchestratorMock = null!;
    private ShoppingAgentMetrics _metrics = null!;
    private IOptions<AgentOptions> _agentOptions = null!;
    private IOptions<LlmClientOptions> _llmOptions = null!;
    private ConversationManager _sut = null!;
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _dispatcherMock = Substitute.For<IToolCallDispatcher>();
        _llmCommunicatorMock = Substitute.For<ILlmCommunicator>();
        _toolExecutionOrchestratorMock = Substitute.For<IToolExecutionOrchestrator>();

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        _metrics = new ShoppingAgentMetrics(meterFactory);

        _agentOptions = Options.Create(new AgentOptions());
        _llmOptions = Options.Create(new LlmClientOptions());

        _sut = new ConversationManager(
            _dispatcherMock,
            _llmCommunicatorMock,
            _toolExecutionOrchestratorMock,
            NullLogger<ConversationManager>.Instance,
            _metrics,
            _agentOptions,
            _llmOptions);

        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ShoppingAgentDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

    [Test]
    public async Task ProcessAsync_ReturnsNewMessagesWithoutMutatingInputHistory()
    {
        // Arrange
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello from the agent")]);
        var appendedMessage = new ChatMessage(ChatRole.Assistant, "Persisted assistant message");

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(ChatResponse?, string?, string?)>((response, null, "fallback")));
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(
                callInfo.ArgAt<IList<ChatMessage>>(1),
                callInfo.ArgAt<ConversationProcessingState>(3),
                appendedMessage,
                true,
                "processed"));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var results = new List<string>();
        await foreach (var chunk in result.Chunks)
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainInOrder("fallback", "processed");
        history.Should().ContainSingle();
        result.NewMessages.Should().ContainSingle().Which.Should().BeSameAs(appendedMessage);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ShoppingAgent.ProcessMessage");
        _completedActivities.Single().Tags.Should().Contain(tag => tag.Key == "agent.shop" && tag.Value == "coop");
    }

    [Test]
    public async Task ProcessAsync_ReturnsNewMessages_WhenNoActivityListenerIsRegistered()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `processActivity?.SetTag` branch used for optional OpenTelemetry tagging.
        _activityListener.Dispose();
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello from the agent")]);
        var appendedMessage = new ChatMessage(ChatRole.Assistant, "Persisted assistant message");

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(ChatResponse?, string?, string?)>((response, null, null)));
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(
                callInfo.ArgAt<IList<ChatMessage>>(1),
                callInfo.ArgAt<ConversationProcessingState>(3),
                appendedMessage,
                true,
                "processed"));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var results = new List<string>();
        await foreach (var chunk in result.Chunks)
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().Contain("processed");
        result.NewMessages.Should().ContainSingle().Which.Should().BeSameAs(appendedMessage);
    }

    [Test]
    public async Task ProcessAsync_StopsAfterErrorMessage_WhenNoActivityListenerIsRegistered()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `processActivity?.SetStatus` branch on the error path.
        _activityListener.Dispose();
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(ChatResponse?, string?, string?)>((null, "llm-error", null)));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var results = new List<string>();
        await foreach (var chunk in result.Chunks)
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("llm-error");
    }

    [Test]
    public async Task ProcessAsync_StopsAfterErrorMessage()
    {
        // Arrange
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(ChatResponse?, string?, string?)>((null, "llm-error", null)));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var results = new List<string>();
        await foreach (var chunk in result.Chunks)
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainSingle().Which.Should().Be("llm-error");
        _toolExecutionOrchestratorMock.DidNotReceive().ProcessResponseAsync(
            Arg.Any<ChatResponse>(),
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<string>(),
            Arg.Any<ConversationProcessingState>(),
            Arg.Any<CancellationToken>());
        _completedActivities.Should().ContainSingle().Which.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Test]
    public async Task ProcessAsync_ContinuesIterationsUntilBreakIsRequested()
    {
        // Arrange
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);
        var invocationCount = 0;

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                invocationCount++;
                return Task.FromResult<(ChatResponse?, string?, string?)>((response, null, null));
            });
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(callInfo.ArgAt<ConversationProcessingState>(3), invocationCount >= 2, $"chunk-{invocationCount.ToString(CultureInfo.InvariantCulture)}"));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var results = new List<string>();
        await foreach (var chunk in result.Chunks)
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().ContainInOrder("chunk-1", "chunk-2");
        await _llmCommunicatorMock.Received(2).GetResponseAsync(
            chatClient,
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<Func<IReadOnlyList<AITool>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_StopsAfterMaxToolCallingIterations_WhenNoBreakIsRequested()
    {
        // Arrange — proves the for-loop's iteration counter actually increases; with a mutated
        // decrement, the loop would never reach MaxToolCallingIterations and run indefinitely.
        _agentOptions = Options.Create(new AgentOptions { MaxToolCallingIterations = 3 });
        _sut = new ConversationManager(
            _dispatcherMock,
            _llmCommunicatorMock,
            _toolExecutionOrchestratorMock,
            NullLogger<ConversationManager>.Instance,
            _metrics,
            _agentOptions,
            _llmOptions);

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);
        var invocationCount = 0;

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                invocationCount++;
                if (invocationCount > 3)
                {
                    throw new InvalidOperationException("Too many iterations");
                }

                return Task.FromResult<(ChatResponse?, string?, string?)>((response, null, null));
            });
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(callInfo.ArgAt<ConversationProcessingState>(3), false));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        await ConsumeChunksAsync(result.Chunks).WaitAsync(TimeSpan.FromSeconds(1));

        // Assert — the loop must terminate exactly after MaxToolCallingIterations calls.
        await _llmCommunicatorMock.Received(3).GetResponseAsync(
            chatClient,
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<Func<IReadOnlyList<AITool>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_RecordsMessageProcessingTimeMetric()
    {
        // Arrange
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(ChatResponse?, string?, string?)>((response, null, null)));
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(callInfo.ArgAt<ConversationProcessingState>(3), true));

        var recordedValues = new List<double>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Name, _metrics.MessageProcessingTimeMs.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<double>((_, measurement, _, _) => recordedValues.Add(measurement));
        meterListener.Start();

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        await foreach (var chunk in result.Chunks)
        {
            _ = chunk;
        }

        // Assert
        recordedValues.Should().ContainSingle();
    }

    [Test]
    public async Task ProcessAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () =>
        {
            var result = _sut.ProcessAsync(history, chatClient, () => [], "coop", cts.Token);
            await foreach (var chunk in result.Chunks.WithCancellation(cts.Token))
            {
                _ = chunk;
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task ProcessAsync_WhenCancelledMidStream_StillReturnsMessagesProducedBeforeCancellation()
    {
        // Arrange — regression test: a cancellation (or any exception) after at least one
        // successful iteration must not discard tool-call/assistant messages that iteration
        // already produced, since some of those calls (e.g. add_to_cart) have real side effects
        // that the next turn needs to see in the conversation history.
        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello from the agent")]);
        var appendedMessage = new ChatMessage(ChatRole.Assistant, "Message from the completed first iteration");
        var invocationCount = 0;

        _llmCommunicatorMock
            .GetResponseAsync(Arg.Any<IChatClient>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<Func<IReadOnlyList<AITool>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                invocationCount++;
                if (invocationCount > 1)
                {
                    throw new OperationCanceledException("Cancelled mid-stream");
                }

                return Task.FromResult<(ChatResponse?, string?, string?)>((response, null, null));
            });
        _toolExecutionOrchestratorMock
            .ProcessResponseAsync(Arg.Any<ChatResponse>(), Arg.Any<IList<ChatMessage>>(), Arg.Any<string>(), Arg.Any<ConversationProcessingState>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReturnChunks(
                callInfo.ArgAt<IList<ChatMessage>>(1),
                callInfo.ArgAt<ConversationProcessingState>(3),
                appendedMessage,
                false,
                "chunk1"));

        // Act
        var result = _sut.ProcessAsync(history, chatClient, () => [], "coop");
        var act = async () =>
        {
            await foreach (var chunk in result.Chunks)
            {
                _ = chunk;
            }
        };

        // Assert — the exception still propagates, but the first iteration's message is preserved.
        await act.Should().ThrowAsync<OperationCanceledException>();
        result.NewMessages.Should().ContainSingle().Which.Should().BeSameAs(appendedMessage);
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
        // Arrange

        // Act
        _sut.ResetWorkflow();

        // Assert
        _dispatcherMock.Received(1).ResetWorkflow();
    }

    private static async IAsyncEnumerable<string> ReturnChunks(
        IList<ChatMessage> workingHistory,
        ConversationProcessingState state,
        ChatMessage appendedMessage,
        bool shouldBreak,
        params string[] chunks)
    {
        workingHistory.Add(appendedMessage);

        foreach (var chunk in chunks)
        {
            yield return chunk;
        }

        if (shouldBreak)
        {
            state.ShouldBreak = true;
        }

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<string> ReturnChunks(
        ConversationProcessingState state,
        bool shouldBreak,
        params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
        }

        if (shouldBreak)
        {
            state.ShouldBreak = true;
        }

        await Task.CompletedTask;
    }

    private static async Task ConsumeChunksAsync(IAsyncEnumerable<string> chunks)
    {
        await foreach (var chunk in chunks)
        {
            _ = chunk;
        }
    }
}
