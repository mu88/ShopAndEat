using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.Metrics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Services.Concrete;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ResilientChatClientTests
{
    private readonly List<(string Name, int Value)> _recordedCounterMeasurements = [];
    private MeterListener? _meterListener;

    [SetUp]
    public void SetUp()
    {
        _recordedCounterMeasurements.Clear();
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
        _meterListener.SetMeasurementEventCallback<int>(
            (instrument, measurement, _, _) => _recordedCounterMeasurements.Add((instrument.Name, measurement)));
        _meterListener.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _meterListener?.Dispose();
    }

    [Test]
    public async Task GetResponseAsync_CallsPrimary_WhenRetryDisabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var expectedResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "response")]);
        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        var testee = CreateTestee(
            primaryMock,
            null,
            retryEnabled: false,
            fallbackEnabled: false);

        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var result = await testee.GetResponseAsync(messages);

        // Assert
        result.Should().Be(expectedResponse);
        await primaryMock.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_DoesNotEnterRetryPipeline_WhenRetryDisabled_EvenOnRateLimit()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(rateLimitException));

        var testee = CreateTestee(
            primaryMock,
            retryEnabled: false,
            retryMaxAttempts: 3,
            retryBaseDelayMs: 1,
            fallbackEnabled: false);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        await testee.Invoking(t => t.GetResponseAsync(messages))
            .Should()
            .ThrowAsync<ClientResultException>();

        // A single call proves the retry pipeline was bypassed entirely (no retries, no fallback attempt).
        await primaryMock.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_PropagatesRateLimitException_WhenFallbackEnabledButNoFallbackClient()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(rateLimitException));

        var testee = CreateTestee(
            primaryMock,
            fallbackClient: null,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        // With fallback enabled but no fallback client configured, the original rate limit exception
        // must propagate (proves the fallback branch requires BOTH conditions, not just one of them).
        await testee.Invoking(t => t.GetResponseAsync(messages))
            .Should()
            .ThrowAsync<ClientResultException>();
    }

    [Test]
    public async Task GetResponseAsync_ReturnsResponse_WhenPrimarySucceeds()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var expectedResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "response")]);
        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        var testee = CreateTestee(primaryMock);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var result = await testee.GetResponseAsync(messages);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Test]
    public async Task GetResponseAsync_RetriesOnRateLimitAndSucceeds()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var expectedResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "success")]);

        var rateLimitException = CreateRateLimitException();

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ChatResponse>(rateLimitException),
                Task.FromResult(expectedResponse));

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 2, retryBaseDelayMs: 1);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var result = await testee.GetResponseAsync(messages);

        // Assert
        result.Should().Be(expectedResponse);
        await primaryMock.Received(2).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_ThrowsAfterRetriesExhausted_WhenFallbackDisabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(rateLimitException));

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 2, retryBaseDelayMs: 1, fallbackEnabled: false);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        await testee.Invoking(t => t.GetResponseAsync(messages))
            .Should()
            .ThrowAsync<ClientResultException>();

        await primaryMock.Received(2).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_UsesFallback_WhenRetriesExhaustedAndFallbackEnabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var fallbackMock = Substitute.For<IChatClient>();

        var rateLimitException = CreateRateLimitException();
        var fallbackResponse = new ChatResponse([new AiChatMessage(ChatRole.Assistant, "fallback response")]);

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(rateLimitException));

        fallbackMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fallbackResponse));

        var testee = CreateTestee(
            primaryMock,
            fallbackMock,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var result = await testee.GetResponseAsync(messages);

        // Assert
        result.Should().Be(fallbackResponse);
        await primaryMock.Received(2).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        await fallbackMock.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        _recordedCounterMeasurements.Should().ContainSingle(measurement =>
            measurement.Name == "shopping_agent.llm.model_fallbacks_total" && measurement.Value == 1);
    }

    [Test]
    public async Task GetResponseAsync_PropagatesFallbackException_WhenFallbackClientFails()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var fallbackMock = Substitute.For<IChatClient>();

        var rateLimitException = CreateRateLimitException();
        var fallbackException = new InvalidOperationException("Fallback model unavailable");

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(rateLimitException));

        fallbackMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(fallbackException));

        var testee = CreateTestee(
            primaryMock,
            fallbackMock,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        await testee.Invoking(t => t.GetResponseAsync(messages))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Fallback model unavailable");
    }

    [Test]
    public async Task GetResponseAsync_PropagatesNon429Errors_Immediately()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var someException = new InvalidOperationException("Some error");

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(someException));

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 3);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        await testee.Invoking(t => t.GetResponseAsync(messages))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await primaryMock.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetResponseAsync_PropagatesCancellation_WithoutRetry()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var cts = new CancellationTokenSource();

        primaryMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return await Task.FromException<ChatResponse>(new OperationCanceledException());
            });

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 3);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        await testee.Invoking(t => t.GetResponseAsync(messages, null, cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        await primaryMock.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetStreamingResponseAsync_YieldsPrimaryChunks_WhenRetryDisabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateStream("chunk1", "chunk2"));

        var testee = CreateTestee(primaryMock, retryEnabled: false, fallbackEnabled: false);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var chunks = await CollectAsync(testee.GetStreamingResponseAsync(messages));

        // Assert
        chunks.Should().HaveCount(2);
    }

    [Test]
    public async Task GetStreamingResponseAsync_YieldsPrimaryChunks_WhenPrimarySucceeds()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateStream("chunk1"));

        var testee = CreateTestee(primaryMock);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var chunks = await CollectAsync(testee.GetStreamingResponseAsync(messages));

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Test]
    public async Task GetStreamingResponseAsync_RetriesOnRateLimitAndSucceeds()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => throw rateLimitException,
                _ => CreateStream("chunk1"));

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 2, retryBaseDelayMs: 1);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var chunks = await CollectAsync(testee.GetStreamingResponseAsync(messages));

        // Assert
        chunks.Should().HaveCount(1);
        primaryMock.Received(2).GetStreamingResponseAsync(
            Arg.Any<IEnumerable<AiChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetStreamingResponseAsync_ThrowsAfterRetriesExhausted_WhenFallbackDisabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw rateLimitException);

        var testee = CreateTestee(primaryMock, retryMaxAttempts: 2, retryBaseDelayMs: 1, fallbackEnabled: false);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        var act = () => CollectAsync(testee.GetStreamingResponseAsync(messages));
        await act.Should().ThrowAsync<ClientResultException>();
    }

    [Test]
    public async Task GetStreamingResponseAsync_PropagatesRateLimitException_WhenFallbackEnabledButNoFallbackClient()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();

        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw rateLimitException);

        var testee = CreateTestee(
            primaryMock,
            fallbackClient: null,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        // With fallback enabled but no fallback client configured, the original rate limit exception
        // must propagate (proves the fallback branch requires BOTH conditions, not just one of them).
        var act = () => CollectAsync(testee.GetStreamingResponseAsync(messages));
        await act.Should().ThrowAsync<ClientResultException>();
    }

    [Test]
    public async Task GetStreamingResponseAsync_UsesFallback_WhenRetriesExhaustedAndFallbackEnabled()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var fallbackMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();
        var testLogger = new TestLogger<ResilientChatClient>();

        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw rateLimitException);

        fallbackMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateStream("fallback-chunk"));

        var testee = CreateTestee(
            primaryMock,
            fallbackMock,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true,
            logger: testLogger);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act
        var chunks = await CollectAsync(testee.GetStreamingResponseAsync(messages));

        // Assert
        chunks.Should().ContainSingle().Which.Text.Should().Be("fallback-chunk");
        _recordedCounterMeasurements.Should().ContainSingle(measurement =>
            measurement.Name == "shopping_agent.llm.model_fallbacks_total" && measurement.Value == 1);
        testLogger.Messages.Should().Contain("Fallback model call succeeded");
        testLogger.Messages.Should().NotContain(message => message.Contains("Fallback model call failed", StringComparison.Ordinal));
    }

    [Test]
    public async Task GetStreamingResponseAsync_LogsFallbackFailure_WhenFallbackStreamThrows()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var fallbackMock = Substitute.For<IChatClient>();
        var rateLimitException = CreateRateLimitException();
        var testLogger = new TestLogger<ResilientChatClient>();

        primaryMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw rateLimitException);

        fallbackMock.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CreateFailingStream());

        var testee = CreateTestee(
            primaryMock,
            fallbackMock,
            retryMaxAttempts: 2,
            retryBaseDelayMs: 1,
            fallbackEnabled: true,
            logger: testLogger);
        var messages = new[] { new AiChatMessage(ChatRole.User, "test") };

        // Act & Assert
        var act = () => CollectAsync(testee.GetStreamingResponseAsync(messages));
        await act.Should().ThrowAsync<InvalidOperationException>();
        testLogger.Messages.Should().Contain("Fallback model call failed: Stream interrupted");
        testLogger.Messages.Should().NotContain("Fallback model call succeeded");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStream(params string[] texts)
    {
        foreach (var text in texts)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateFailingStream()
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
        await Task.Yield();
        throw new InvalidOperationException("Stream failed");
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(IAsyncEnumerable<ChatResponseUpdate> source)
    {
        var result = new List<ChatResponseUpdate>();
        await foreach (var chunk in source)
        {
            result.Add(chunk);
        }

        return result;
    }

    private static ResilientChatClient CreateTestee(
        IChatClient primaryClient,
        IChatClient? fallbackClient = null,
        int retryMaxAttempts = 3,
        int retryBaseDelayMs = 1000,
        bool retryEnabled = true,
        bool fallbackEnabled = false,
        ILogger<ResilientChatClient>? logger = null)
    {
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = retryMaxAttempts,
            RetryBaseDelayMs = retryBaseDelayMs,
        });

        var agentOptions = Options.Create(new AgentOptions
        {
            RetryEnabled = retryEnabled,
            ModelFallbackEnabled = fallbackEnabled,
        });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var retryPolicyFactory = new LlmRetryPolicyFactory(llmOptions, metrics, NullLogger<LlmRetryPolicyFactory>.Instance);

        return new ResilientChatClient(
            primaryClient,
            fallbackClient,
            logger ?? NullLogger<ResilientChatClient>.Instance,
            llmOptions.Value,
            agentOptions.Value,
            metrics,
            retryPolicyFactory);
    }

    /// <summary>
    /// Minimal <see cref="ILogger{T}"/> test double that records formatted log messages,
    /// used to distinguish which branch of the fallback-stream success/failure logging was taken.
    /// </summary>
    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    [Test]
    public void GetService_ReturnsPrimaryClientService_WhenTypeMatches()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var serviceMock = Substitute.For<IAsyncDisposable>();
        primaryMock.GetService(typeof(IAsyncDisposable), null).Returns(serviceMock);

        var testee = CreateTestee(primaryMock);

        // Act
        var result = testee.GetService(typeof(IAsyncDisposable), null);

        // Assert
        result.Should().Be(serviceMock);
        primaryMock.Received(1).GetService(typeof(IAsyncDisposable), null);
    }

    [Test]
    public void GetService_ReturnsPrimaryClientService_WithServiceKey()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var serviceMock = Substitute.For<IAsyncDisposable>();
        var serviceKey = "test-key";
        primaryMock.GetService(typeof(IAsyncDisposable), serviceKey).Returns(serviceMock);

        var testee = CreateTestee(primaryMock);

        // Act
        var result = testee.GetService(typeof(IAsyncDisposable), serviceKey);

        // Assert
        result.Should().Be(serviceMock);
        primaryMock.Received(1).GetService(typeof(IAsyncDisposable), serviceKey);
    }

    [Test]
    public void GetService_ReturnsNull_WhenServiceNotAvailable()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        primaryMock.GetService(Arg.Any<Type>(), Arg.Any<object>()).Returns((object?)null);

        var testee = CreateTestee(primaryMock);

        // Act
        var result = testee.GetService(typeof(IAsyncDisposable), null);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(primaryMock);

        // Act
        var action = () => testee.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var primaryMock = Substitute.For<IChatClient>();
        var testee = CreateTestee(primaryMock);

        // Act
        var action = () =>
        {
            testee.Dispose();
            testee.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    private static ClientResultException CreateRateLimitException()
    {
        var exception = new ClientResultException("Too Many Requests", null, null);
        var statusProperty = typeof(ClientResultException).GetProperty("Status");
        if (statusProperty is not null)
        {
            var setter = statusProperty.GetSetMethod(nonPublic: true);
            if (setter is not null)
            {
                setter.Invoke(exception, new object[] { (int)HttpStatusCode.TooManyRequests });
            }
        }

        return exception;
    }
}
