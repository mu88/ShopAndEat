using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.Metrics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.AI;
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
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromException<ChatResponse>(new OperationCanceledException());
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

    private static ResilientChatClient CreateTestee(
        IChatClient primaryClient,
        IChatClient fallbackClient = null,
        int retryMaxAttempts = 3,
        int retryBaseDelayMs = 1000,
        bool retryEnabled = true,
        bool fallbackEnabled = false)
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
            NullLogger<ResilientChatClient>.Instance,
            llmOptions.Value,
            agentOptions.Value,
            metrics,
            retryPolicyFactory);
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
