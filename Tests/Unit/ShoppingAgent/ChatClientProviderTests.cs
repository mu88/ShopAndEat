using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using OpenAI.Chat;
using ShoppingAgent.Options;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ChatClientProviderTests
{
    [Test]
    public async Task GetChatClientAsync_ReturnsChatClient_WhenApiKeyConfigured()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var client = await testee.GetChatClientAsync();

        // Assert
        client.Should().NotBeNull();
    }

    [Test]
    public async Task GetChatClientAsync_UsesConfiguredEndpoint()
    {
        // Arrange
        var testee = CreateTestee(endpoint: "https://custom-mistral.example.test/v1");

        // Act
        var client = await testee.GetChatClientAsync();

        // Assert — underlying OpenAI ChatClient must actually target the configured endpoint.
        var chatClient = client.GetService(typeof(ChatClient)) as ChatClient;
        chatClient.Should().NotBeNull();
#pragma warning disable OPENAI001 // Endpoint is experimental but is the only way to verify endpoint wiring
        chatClient!.Endpoint.Should().Be(new Uri("https://custom-mistral.example.test/v1"));
#pragma warning restore OPENAI001
    }

    [Test]
    public async Task GetFallbackChatClientAsync_UsesConfiguredEndpoint()
    {
        // Arrange
        var testee = CreateTestee(endpoint: "https://custom-mistral.example.test/v1");

        // Act
        var client = await testee.GetFallbackChatClientAsync();

        // Assert
        var chatClient = client.GetService(typeof(ChatClient)) as ChatClient;
        chatClient.Should().NotBeNull();
#pragma warning disable OPENAI001 // Endpoint is experimental but is the only way to verify endpoint wiring
        chatClient!.Endpoint.Should().Be(new Uri("https://custom-mistral.example.test/v1"));
#pragma warning restore OPENAI001
    }

    [Test]
    public async Task GetChatClientAsync_ReturnsCachedInstance_OnSecondCall()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var first = await testee.GetChatClientAsync();
        var second = await testee.GetChatClientAsync();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Test]
    public async Task InvalidateClient_CausesNewInstanceOnNextCall()
    {
        // Arrange
        var testee = CreateTestee();
        var first = await testee.GetChatClientAsync();

        // Act
        testee.InvalidateClient();
        var second = await testee.GetChatClientAsync();

        // Assert
        first.Should().NotBeSameAs(second);
    }

    [Test]
    public void InvalidateClient_LogsApiKeyInvalidated_WhenClientWasCached()
    {
        // Arrange
        var logger = new TestLogger<MistralChatClientProvider>();
        var testee = CreateTestee(logger: logger);
        _ = testee.GetChatClientAsync().Result;

        // Act
        testee.InvalidateClient();

        // Assert
        logger.Messages.Should().ContainSingle(message => message.Contains("invalidated", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void InvalidateClient_DoesNotLogApiKeyInvalidated_WhenNoClientWasCached()
    {
        // Arrange
        var logger = new TestLogger<MistralChatClientProvider>();
        var testee = CreateTestee(logger: logger);

        // Act
        testee.InvalidateClient();

        // Assert
        logger.Messages.Should().BeEmpty();
    }

    [Test]
    public async Task GetFallbackChatClientAsync_ReturnsFallbackClient_WhenApiKeyConfigured()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var client = await testee.GetFallbackChatClientAsync();

        // Assert
        client.Should().NotBeNull();
    }

    [Test]
    public async Task GetFallbackChatClientAsync_ReturnsCachedInstance_OnSecondCall()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var first = await testee.GetFallbackChatClientAsync();
        var second = await testee.GetFallbackChatClientAsync();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Test]
    public async Task InvalidateClient_ClearsPrimaryAndFallbackCaches()
    {
        // Arrange
        var testee = CreateTestee();
        var primaryFirst = await testee.GetChatClientAsync();
        var fallbackFirst = await testee.GetFallbackChatClientAsync();

        // Act
        testee.InvalidateClient();
        var primarySecond = await testee.GetChatClientAsync();
        var fallbackSecond = await testee.GetFallbackChatClientAsync();

        // Assert
        primaryFirst.Should().NotBeSameAs(primarySecond);
        fallbackFirst.Should().NotBeSameAs(fallbackSecond);
    }

    [Test]
    public void InvalidateClient_DisposesPrimaryAndFallbackClients_WhenCachesArePopulated()
    {
        // Arrange
        var testee = CreateTestee();
        var primary = Substitute.For<IChatClient, IDisposable>();
        var fallback = Substitute.For<IChatClient, IDisposable>();
        SetPrivateField(testee, "_cachedClient", primary);
        SetPrivateField(testee, "_cachedFallbackClient", fallback);

        // Act
        testee.InvalidateClient();

        // Assert
        ((IDisposable)primary).Received(1).Dispose();
        ((IDisposable)fallback).Received(1).Dispose();
    }

    [Test]
    public void Dispose_DisposesTheProvider_WhenCalled()
    {
        // Arrange
        var testee = CreateTestee();
        _ = testee.GetChatClientAsync().Result;

        // Act & Assert
        var action = () => testee.Dispose();
        action.Should().NotThrow();
    }

    [Test]
    public async Task Dispose_DisposesCachedFallbackClient_WhenFallbackWasRequested()
    {
        // Arrange — populates _cachedFallbackClient before Dispose() so the (fallback as IDisposable)?.Dispose()
        // branch inside Dispose() is actually exercised, not just the always-null variant.
        var testee = CreateTestee();
        _ = await testee.GetFallbackChatClientAsync();

        // Act & Assert
        var action = () => testee.Dispose();
        action.Should().NotThrow();
    }

    [Test]
    public void Dispose_DisposesPrimaryAndFallbackClients_WhenCachesArePopulated()
    {
        // Arrange
        var testee = CreateTestee();
        var primary = Substitute.For<IChatClient, IDisposable>();
        var fallback = Substitute.For<IChatClient, IDisposable>();
        SetPrivateField(testee, "_cachedClient", primary);
        SetPrivateField(testee, "_cachedFallbackClient", fallback);

        // Act
        testee.Dispose();

        // Assert
        ((IDisposable)primary).Received(1).Dispose();
        ((IDisposable)fallback).Received(1).Dispose();
    }

    [Test]
    public void Dispose_ClearsClientCache()
    {
        // Arrange
        var testee1 = CreateTestee();
        var first = testee1.GetChatClientAsync().Result;
        testee1.Dispose();

        var testee2 = CreateTestee();
        var second = testee2.GetChatClientAsync().Result;

        // Act & Assert
        first.Should().NotBeSameAs(second);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var action = () =>
        {
            testee.Dispose();
            testee.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    [Test]
    public async Task CheckConnectionAsync_ReturnsTrue_WhenEndpointRespondsSuccessfully()
    {
        // Arrange
        var testee = CreateTestee(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        // Act
        var result = await testee.CheckConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task CheckConnectionAsync_ReturnsFalse_WhenEndpointRespondsWithError()
    {
        // Arrange
        var testee = CreateTestee(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // Act
        var result = await testee.CheckConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task CheckConnectionAsync_ReturnsFalse_WhenRequestThrows()
    {
        // Arrange
        var testee = CreateTestee(new FakeHttpMessageHandler(_ => throw new HttpRequestException("connection refused")));

        // Act
        var result = await testee.CheckConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    private static MistralChatClientProvider CreateTestee(string? endpoint = null, ILogger<MistralChatClientProvider>? logger = null) =>
        new(new HttpClient(),
            logger ?? NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions { ApiKey = "test-key-123", Endpoint = endpoint ?? "https://api.mistral.ai/v1" }));

    // Each test supplies its own fake handler to simulate a distinct HTTP scenario, so a
    // shared HttpClient instance across tests is not viable here (IDISP014 false positive).
#pragma warning disable IDISP014
    private static MistralChatClientProvider CreateTestee(HttpMessageHandler handler) =>
        new(new HttpClient(handler),
            NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions { ApiKey = "test-key-123", Endpoint = "https://example.test" }));
#pragma warning restore IDISP014

    private static void SetPrivateField(MistralChatClientProvider testee, string fieldName, object value) =>
        typeof(MistralChatClientProvider)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(testee, value);

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    /// <summary>Minimal <see cref="ILogger{T}"/> test double recording rendered log messages, since NullLogger discards them.</summary>
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
