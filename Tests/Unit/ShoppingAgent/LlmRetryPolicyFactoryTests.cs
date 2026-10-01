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

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class LlmRetryPolicyFactoryTests
{
    [Test]
    public async Task CreateChatResponsePipeline_When429RetryMaxAttemptsThree_PerformsThreeTotalAttempts()
    {
        // Arrange
        var factory = CreateTestee(retryMaxAttempts: 3, retryBaseDelayMs: 1);
        var pipeline = factory.CreateChatResponsePipeline();
        var attempts = 0;
        var rateLimitException = CreateRateLimitException();

        // Act
        Func<Task> action = async () =>
            await pipeline.ExecuteAsync(_ =>
            {
                attempts++;
                return ValueTask.FromException<ChatResponse>(rateLimitException);
            }, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ClientResultException>();
        attempts.Should().Be(3);
    }

    [Test]
    public async Task CreateChatResponsePipeline_When429ThenSuccess_RetriesAndReturnsResponse()
    {
        // Arrange
        var factory = CreateTestee(retryMaxAttempts: 3, retryBaseDelayMs: 1);
        var pipeline = factory.CreateChatResponsePipeline();
        var attempts = 0;
        var expected = new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]);

        // Act
        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRateLimitException();
            }

            return ValueTask.FromResult(expected);
        }, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        attempts.Should().Be(2);
    }

    [Test]
    public async Task CreateChatResponsePipeline_WhenNon429Error_DoesNotRetry()
    {
        // Arrange
        var factory = CreateTestee(retryMaxAttempts: 3, retryBaseDelayMs: 1);
        var pipeline = factory.CreateChatResponsePipeline();
        var attempts = 0;

        // Act
        Func<Task> action = async () =>
            await pipeline.ExecuteAsync(_ =>
            {
                attempts++;
                return ValueTask.FromException<ChatResponse>(new InvalidOperationException("boom"));
            }, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Test]
    public async Task CreateChatResponsePipeline_When429_IncrementsRetriesTotalMetric()
    {
        // Arrange
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = 3,
            RetryBaseDelayMs = 1,
        });
        var factory = new LlmRetryPolicyFactory(llmOptions, metrics, NullLogger<LlmRetryPolicyFactory>.Instance);
        var pipeline = factory.CreateChatResponsePipeline();
        var attempts = 0;

        var recordedValues = new List<int>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Name, metrics.RetriesTotal.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<int>((_, measurement, _, _) => recordedValues.Add(measurement));
        meterListener.Start();

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRateLimitException();
            }

            return ValueTask.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]));
        }, CancellationToken.None);

        // Assert
        recordedValues.Should().ContainSingle().Which.Should().Be(1);
    }

    [Test]
    public async Task CreateChatResponsePipeline_When429_LogsCorrectOneBasedAttemptNumber()
    {
        // Arrange — proves attempt = args.AttemptNumber + 1 (one-based); Polly's AttemptNumber for
        // the first retry is 0, so the logged attempt for that retry must be 1, not -1.
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = 3,
            RetryBaseDelayMs = 1,
        });
        var testLogger = new TestLogger<LlmRetryPolicyFactory>();
        var factory = new LlmRetryPolicyFactory(llmOptions, metrics, testLogger);
        var pipeline = factory.CreateChatResponsePipeline();
        var attempts = 0;

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRateLimitException();
            }

            return ValueTask.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]));
        }, CancellationToken.None);

        // Assert
        testLogger.Messages.Should().ContainSingle(message => message.Contains("attempt 1/3", StringComparison.Ordinal));
    }

    [Test]
    public async Task CreateStreamingStartPipeline_When429_IncrementsRetriesTotalMetric()
    {
        // Arrange
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = 3,
            RetryBaseDelayMs = 1,
        });
        var factory = new LlmRetryPolicyFactory(llmOptions, metrics, NullLogger<LlmRetryPolicyFactory>.Instance);
        var pipeline = factory.CreateStreamingStartPipeline();
        var attempts = 0;

        var recordedValues = new List<int>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Name, metrics.RetriesTotal.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<int>((_, measurement, _, _) => recordedValues.Add(measurement));
        meterListener.Start();

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRateLimitException();
            }

            return ValueTask.FromResult(CreateEmptyUpdateStream());
        }, CancellationToken.None);

        // Assert
        recordedValues.Should().ContainSingle().Which.Should().Be(1);
    }

    [Test]
    public async Task CreateStreamingStartPipeline_When429_LogsCorrectOneBasedAttemptNumber()
    {
        // Arrange — proves attempt = args.AttemptNumber + 1 (one-based) for the streaming pipeline too.
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = 3,
            RetryBaseDelayMs = 1,
        });
        var testLogger = new TestLogger<LlmRetryPolicyFactory>();
        var factory = new LlmRetryPolicyFactory(llmOptions, metrics, testLogger);
        var pipeline = factory.CreateStreamingStartPipeline();
        var attempts = 0;

        // Act
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRateLimitException();
            }

            return ValueTask.FromResult(CreateEmptyUpdateStream());
        }, CancellationToken.None);

        // Assert
        testLogger.Messages.Should().ContainSingle(message => message.Contains("attempt 1/3", StringComparison.Ordinal));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateEmptyUpdateStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static LlmRetryPolicyFactory CreateTestee(int retryMaxAttempts, int retryBaseDelayMs)
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));
        var metrics = new ShoppingAgentMetrics(meterFactory);
        var llmOptions = Options.Create(new LlmClientOptions
        {
            ApiKey = "test-key",
            RetryMaxAttempts = retryMaxAttempts,
            RetryBaseDelayMs = retryBaseDelayMs,
        });

        return new LlmRetryPolicyFactory(llmOptions, metrics, NullLogger<LlmRetryPolicyFactory>.Instance);
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

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
