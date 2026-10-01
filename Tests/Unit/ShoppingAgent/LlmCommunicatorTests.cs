using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class LlmCommunicatorTests
{
    private readonly List<Activity> _completedActivities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _doubleMeasurements = [];
    private IStringLocalizer<Messages> _localizerMock = null!;
    private ShoppingAgentMetrics _metrics = null!;
    private IOptions<LlmClientOptions> _llmOptions = null!;
    private LlmCommunicator _sut = null!;
    private ActivityListener _activityListener = null!;
    private MeterListener _meterListener = null!;

    [SetUp]
    public void SetUp()
    {
        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        _localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        _metrics = new ShoppingAgentMetrics(meterFactory);

        _llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key" });
        _sut = new LlmCommunicator(_localizerMock, NullLogger<LlmCommunicator>.Instance, _metrics, _llmOptions);

        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ShoppingAgentDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);

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
    public async Task GetResponseAsync_PassesTools_WhenToolCallingIsSupported()
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
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.Response.Should().BeSameAs(response);
        await chatClient.Received(1).GetResponseAsync(
            history,
            Arg.Is<ChatOptions>(options => options.Tools != null && options.Tools.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_WhenToolCallingDisabled_FallsBackWithoutTools()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
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
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.Response.Should().BeSameAs(textResponse);
        result.FallbackMessage.Should().Contain("ToolCallingFallback");
    }

    [Test]
    public async Task GetResponseAsync_WhenToolCallingDisabled_SubsequentCallOmitsTools()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
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

        await _sut.GetResponseAsync(chatClient, history, () => tools);

        history.Clear();
        history.Add(new ChatMessage(ChatRole.User, "Hello again"));
        chatClient.ClearReceivedCalls();

        var nextResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "No tools here")]);
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(nextResponse));

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.Response.Should().BeSameAs(nextResponse);
        await chatClient.Received(1).GetResponseAsync(
            history,
            Arg.Is<ChatOptions>(options => options.Tools == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_WhenFallbackCallTimesOut_ReturnsTimeoutMessage()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        _llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key", TimeoutSeconds = 1 });
        _sut = new LlmCommunicator(_localizerMock, NullLogger<LlmCommunicator>.Instance, _metrics, _llmOptions);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Tool calling is NOT SUPPORTED by this model")),
                DelayThenReturnResponseAsync);

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.Response.Should().BeNull();
        result.ErrorMessage.Should().Contain("LlmTimeout");
        result.FallbackMessage.Should().Contain("ToolCallingFallback");
        var fallbackActivity = _completedActivities.Should().Contain(activity => activity.OperationName == "ShoppingAgent.ToolFallback").Subject;
        fallbackActivity.Status.Should().Be(ActivityStatusCode.Error);
        fallbackActivity.StatusDescription.Should().Be("timeout");
    }

    [Test]
    public async Task GetResponseAsync_WhenFallbackCallFails_ReturnsErrorMessage()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Tool calling is NOT SUPPORTED by this model")),
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Fallback call failed")));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.Response.Should().BeNull();
        result.ErrorMessage.Should().Contain("LlmError");
        result.FallbackMessage.Should().Contain("ToolCallingFallback");
        var fallbackActivity = _completedActivities.Should().Contain(activity => activity.OperationName == "ShoppingAgent.ToolFallback").Subject;
        fallbackActivity.Status.Should().Be(ActivityStatusCode.Error);
        fallbackActivity.StatusDescription.Should().Be("Fallback call failed");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsTimeoutMessage_WhenLlmTimesOut()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        _llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key", TimeoutSeconds = 1 });
        _sut = new LlmCommunicator(_localizerMock, NullLogger<LlmCommunicator>.Instance, _metrics, _llmOptions);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var cancellationToken = callInfo.ArgAt<CancellationToken>(2);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, "should not reach")]);
            });

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        result.ErrorMessage.Should().Contain("LlmTimeout");
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.LlmCall");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("timeout");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsErrorMessage_WhenLlmThrowsGenericException()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new InvalidOperationException("Model overloaded"));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        result.ErrorMessage.Should().Contain("LlmError");
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Model overloaded");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsResponse_WhenNoActivityListenerIsRegistered_AndCallSucceeds()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetTag` branches used for optional OpenTelemetry tagging.
        _activityListener.Dispose();
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        result.Response.Should().BeSameAs(response);
    }

    [Test]
    public async Task GetResponseAsync_ReturnsTimeoutMessage_WhenNoActivityListenerIsRegistered_AndLlmTimesOut()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch in the timeout/catch path.
        _activityListener.Dispose();
        var chatClient = Substitute.For<IChatClient>();
        _llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key", TimeoutSeconds = 1 });
        _sut = new LlmCommunicator(_localizerMock, NullLogger<LlmCommunicator>.Instance, _metrics, _llmOptions);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var cancellationToken = callInfo.ArgAt<CancellationToken>(2);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, "should not reach")]);
            });

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        result.ErrorMessage.Should().Contain("LlmTimeout");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsErrorMessage_WhenNoActivityListenerIsRegistered_AndLlmThrowsGenericException()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch in the generic-exception catch path.
        _activityListener.Dispose();
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new InvalidOperationException("Model overloaded"));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        result.ErrorMessage.Should().Contain("LlmError");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsTimeoutMessage_WhenNoActivityListenerIsRegistered_AndFallbackCallTimesOut()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch in the fallback call's timeout/catch path.
        _activityListener.Dispose();
        var chatClient = Substitute.For<IChatClient>();
        _llmOptions = Options.Create(new LlmClientOptions { ApiKey = "test-key", TimeoutSeconds = 1 });
        _sut = new LlmCommunicator(_localizerMock, NullLogger<LlmCommunicator>.Instance, _metrics, _llmOptions);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Tool calling is NOT SUPPORTED by this model")),
                DelayThenReturnResponseAsync);

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.ErrorMessage.Should().Contain("LlmTimeout");
    }

    [Test]
    public async Task GetResponseAsync_ReturnsErrorMessage_WhenNoActivityListenerIsRegistered_AndFallbackCallFails()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch in the fallback call's generic-exception catch path.
        _activityListener.Dispose();
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Tool calling is NOT SUPPORTED by this model")),
                _ => Task.FromException<ChatResponse>(new InvalidOperationException("Fallback call failed")));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        result.ErrorMessage.Should().Contain("LlmError");
        result.FallbackMessage.Should().Contain("ToolCallingFallback");
    }

    [Test]
    public async Task GetResponseAsync_RecordsActivityTagsAndMetric_WhenCallSucceeds()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.LlmCall");
        activity.GetTagItem("llm.model").Should().Be(_llmOptions.Value.DefaultModel);
        activity.GetTagItem("llm.tools_enabled").Should().Be(true);
        activity.GetTagItem("llm.status").Should().Be("success");
        activity.Status.Should().Be(ActivityStatusCode.Unset);
        _doubleMeasurements.Should().ContainSingle(measurement => measurement.Name == "shopping_agent.llm.response_time_ms");
    }

    [Test]
    public async Task GetResponseAsync_TagsToolsDisabled_WhenNoToolsProvided()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]);
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        await _sut.GetResponseAsync(chatClient, history, () => []);

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.GetTagItem("llm.tools_enabled").Should().Be(false);
    }

    [Test]
    public async Task GetResponseAsync_RethrowsCancellation_WhenExternalCancellationCoincidesWithInternalTimeout()
    {
        // Arrange: cancelling the caller-supplied token (not the internal LLM timeout, e.g. the Stop
        // button) must propagate as a real OperationCanceledException so the UI can show "Processing
        // stopped." instead of a misleading "LlmError".
        var chatClient = Substitute.For<IChatClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new OperationCanceledException("Caller cancelled"));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };

        // Act
        var act = async () => await _sut.GetResponseAsync(chatClient, history, () => [], cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task GetResponseAsync_RecordsActivityName_ForFallbackCall()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var textResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fallback response")]);
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("Tool calling is NOT SUPPORTED by this model"),
                _ => Task.FromResult(textResponse));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        _completedActivities.Should().Contain(activity => activity.OperationName == "ShoppingAgent.ToolFallback");
    }

    [Test]
    public async Task GetResponseAsync_RethrowsCancellation_WhenExternalCancellationCoincidesWithFallbackTimeout()
    {
        // Arrange: same as the primary-call variant, but for the fallback call path.
        var chatClient = Substitute.For<IChatClient>();
        using var cts = new CancellationTokenSource();

        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(
                _ => throw new InvalidOperationException("Tool calling is NOT SUPPORTED by this model"),
                _ =>
                {
                    cts.Cancel();
                    throw new OperationCanceledException("Caller cancelled during fallback");
                });

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var act = async () => await _sut.GetResponseAsync(chatClient, history, () => tools, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestCase("network TOOL calling error", false)]
    [TestCase("TOOL calling is not supported here", true)]
    [TestCase("TOOL usage is UNSUPPORTED for this model", true)]
    [TestCase("model DOES NOT SUPPORT TOOL usage", true)]
    [TestCase("generic error with NOT SUPPORTED but no keyword", false)]
    [TestCase("tool calling is not supported by this model", true)]
    public async Task GetResponseAsync_TriggersFallback_OnlyWhenMessageContainsToolAndUnsupportedKeyword(
        string exceptionMessage,
        bool shouldFallback)
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var fallbackResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fallback response")]);
        chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException(exceptionMessage),
                _ => Task.FromResult(fallbackResponse));

        var history = new List<ChatMessage> { new(ChatRole.User, "Hi") };
        var tools = new List<AITool> { AIFunctionFactory.Create(() => "test", "test_tool", "A test tool") };

        // Act
        var result = await _sut.GetResponseAsync(chatClient, history, () => tools);

        // Assert
        if (shouldFallback)
        {
            result.Response.Should().BeSameAs(fallbackResponse);
            result.FallbackMessage.Should().Contain("ToolCallingFallback");
        }
        else
        {
            result.Response.Should().BeNull();
            result.ErrorMessage.Should().Contain("LlmError");
            result.FallbackMessage.Should().BeNull();
        }
    }

    private static async Task<ChatResponse> DelayThenReturnResponseAsync(CallInfo callInfo)
    {
        var cancellationToken = callInfo.ArgAt<CancellationToken>(2);
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        return new ChatResponse([new ChatMessage(ChatRole.Assistant, "should not reach")]);
    }
}
