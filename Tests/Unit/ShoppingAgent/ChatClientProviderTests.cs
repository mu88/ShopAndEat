using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
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

    private static MistralChatClientProvider CreateTestee() =>
        new(new HttpClient(),
            NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions { ApiKey = "test-key-123" }));
}
