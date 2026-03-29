using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "Unit tests create isolated HttpClient instances per test by design")]
public class ChatClientProviderTests
{
    [Test]
    public void HasApiKey_IsFalse_Initially()
    {
        // Act
        var testee = CreateTestee();

        // Assert
        testee.HasApiKey.Should().BeFalse();
    }

    [Test]
    public async Task GetChatClientAsync_ThrowsInvalidOperation_WhenNoApiKey()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var act = async () => await testee.GetChatClientAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Test]
    public async Task SetApiKey_MakesClientAvailable()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        testee.ApiKey = "test-key";

        // Assert
        testee.HasApiKey.Should().BeTrue();
        var client = await testee.GetChatClientAsync();
        client.Should().NotBeNull();
    }

    [Test]
    public async Task GetChatClientAsync_ReturnsCachedInstance_OnSecondCall()
    {
        // Arrange
        var testee = CreateTestee();
        testee.ApiKey = "test-key";

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
        testee.ApiKey = "test-key";
        var first = await testee.GetChatClientAsync();

        // Act
        testee.InvalidateClient();
        var second = await testee.GetChatClientAsync();

        // Assert
        first.Should().NotBeSameAs(second);
    }

    [Test]
    public void ClearApiKey_RemovesKey()
    {
        // Arrange
        var testee = CreateTestee();
        testee.ApiKey = "test-key";

        // Act
        testee.ClearApiKey();

        // Assert
        testee.HasApiKey.Should().BeFalse();
    }

    [Test]
    public async Task ClearApiKey_ThrowsOnNextGetChatClient()
    {
        // Arrange
        var testee = CreateTestee();
        testee.ApiKey = "test-key";
        testee.ClearApiKey();

        // Act
        var act = async () => await testee.GetChatClientAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task ApiKey_Setter_DoesNotInvalidateCache_WhenSameKeyAssigned()
    {
        // Arrange
        var testee = CreateTestee();
        testee.ApiKey = "same-key";
        var first = await testee.GetChatClientAsync();

        // Act
        testee.ApiKey = "same-key";
        var second = await testee.GetChatClientAsync();

        // Assert
        first.Should().BeSameAs(second, "re-assigning the same key should not invalidate the cache");
    }

    [Test]
    public async Task ValidateKeyAsync_ReturnsNull_WhenKeyIsValid()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(global::System.Net.HttpStatusCode.OK));
        var testee = new MistralChatClientProvider(
            new HttpClient(handler),
            NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions()));
        testee.ApiKey = "valid-key";

        // Act
        var result = await testee.ValidateKeyAsync();

        // Assert
        result.Should().BeNull();
        testee.HasApiKey.Should().BeTrue();
    }

    [Test]
    public async Task ValidateKeyAsync_ReturnsErrorMessage_WhenKeyIsInvalid()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(global::System.Net.HttpStatusCode.Unauthorized));
        var testee = new MistralChatClientProvider(
            new HttpClient(handler),
            NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions()));
        testee.ApiKey = "invalid-key";

        // Act
        var result = await testee.ValidateKeyAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        testee.HasApiKey.Should().BeFalse();
    }

    [Test]
    public async Task ValidateKeyAsync_ReturnsErrorMessage_WhenNetworkFails()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("DNS error"));
        var testee = new MistralChatClientProvider(
            new HttpClient(handler),
            NullLogger<MistralChatClientProvider>.Instance,
            Options.Create(new LlmClientOptions()));
        testee.ApiKey = "some-key";

        // Act
        var result = await testee.ValidateKeyAsync();

        // Assert
        result.Should().Contain("DNS error");
        testee.HasApiKey.Should().BeFalse();
    }

    private static MistralChatClientProvider CreateTestee() => new(new HttpClient(), NullLogger<MistralChatClientProvider>.Instance, Options.Create(new LlmClientOptions()));

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
