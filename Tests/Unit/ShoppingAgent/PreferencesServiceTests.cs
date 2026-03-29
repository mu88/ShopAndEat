using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class PreferencesServiceTests
{
    [Test]
    public async Task GetAllPreferencesAsync_ReturnsPreferences_OnSuccess()
    {
        // Arrange
        var preferences = new[]
        {
            new { Scope = "global", Key = "prefer_bio", Value = "true", Source = "user_confirmed", StoreKey = (string)null },
            new { Scope = "article:Tofu", Key = "confirmed_product", Value = "Organic Tofu", Source = "agent_learned", StoreKey = "coop" },
        };
        var json = JsonSerializer.Serialize(preferences);
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetAllPreferencesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Scope.Should().Be("global");
        result[0].Key.Should().Be("prefer_bio");
        result[1].StoreKey.Should().Be("coop");
    }

    [Test]
    public async Task GetAllPreferencesAsync_ReturnsEmptyList_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetAllPreferencesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllPreferencesAsync_IncludesStoreKeyInUrl_WhenProvided()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetAllPreferencesAsync("coop");

        // Assert
        handler.LastRequest.RequestUri.ToString().Should().Contain("storeKey=coop");
    }

    [Test]
    public async Task DeletePreferenceAsync_ReturnsFalse_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task DeletePreferenceAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task DeletePreferenceAsync_IncludesStoreKey_WhenProvided()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.DeletePreferenceAsync("article:Tofu", "confirmed_product", "coop");

        // Assert
        handler.LastRequest.RequestUri.ToString().Should().Contain("storeKey=coop");
    }

    [Test]
    public async Task DeletePreferenceAsync_SendsScopeAndKeyAsQueryParams()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.DeletePreferenceAsync("global", "prefer_bio");

        // Assert
        var url = handler.LastRequest.RequestUri!.ToString();
        url.Should().Contain("scope=global");
        url.Should().Contain("key=prefer_bio");
        url.Should().NotContain("storeKey");
    }

    [Test]
    public async Task DeletePreferenceAsync_ReturnsFalse_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_ReturnsPreferences_OnSuccess()
    {
        // Arrange
        var preferences = new[]
        {
            new { Scope = "article:Tofu", Key = "confirmed_product", Value = "Organic Tofu", StoreKey = (string)null },
        };
        var json = JsonSerializer.Serialize(preferences);
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("confirmed_product");
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_SendsArticleScopeAsQueryParam()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        var url = handler.LastRequest.RequestUri!.ToString();
        url.Should().Contain("scope=article%3");
        url.Should().Contain("Tofu");
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_IncludesStoreKey_WhenProvided()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetPreferencesForArticleAsync("Tofu", "coop");

        // Assert
        var url = handler.LastRequest.RequestUri!.ToString();
        url.Should().Contain("storeKey=coop");
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_OmitsStoreKey_WhenNull()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        handler.LastRequest.RequestUri!.ToString().Should().NotContain("storeKey");
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_ReturnsEmptyList_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_ReturnsEmptyList_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SavePreferenceAsync_PostsPreference_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpHandler(request =>
        {
            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);
        var preference = new PreferenceDto { Scope = "global", Key = "prefer_bio", Value = "true" };

        // Act
        await testee.SavePreferenceAsync(preference);

        // Assert
        handler.LastRequest.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Contain("api/preferences");
    }

    [Test]
    public async Task SavePreferenceAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);
        var preference = new PreferenceDto { Scope = "global", Key = "prefer_bio", Value = "true" };

        // Act
        var act = async () => await testee.SavePreferenceAsync(preference);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetAllPreferencesAsync_OmitsStoreKeyInUrl_WhenNotProvided()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetAllPreferencesAsync();

        // Assert
        var url = handler.LastRequest.RequestUri!.ToString();
        url.Should().NotContain("storeKey");
        url.Should().EndWith("api/preferences");
    }

    [Test]
    public async Task GetAllPreferencesAsync_ReturnsEmptyList_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        var result = await testee.GetAllPreferencesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllPreferencesAsync_OmitsStoreKeyInUrl_WhenEmpty()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);

        // Act
        await testee.GetAllPreferencesAsync(string.Empty);

        // Assert
        handler.LastRequest.RequestUri!.ToString().Should().NotContain("storeKey");
    }

    [Test]
    public async Task SavePreferenceAsync_SendsPostToCorrectUrl()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var testee = new PreferencesService(CreateClient(handler), NullLogger<PreferencesService>.Instance);
        var preference = new PreferenceDto { Scope = "article:Milk", Key = "search_term", Value = "Bio Milch" };

        // Act
        await testee.SavePreferenceAsync(preference);

        // Assert
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/app/api/preferences");
    }

    private static HttpClient CreateClient(MockHttpHandler handler)
        => new(handler) { BaseAddress = new Uri("http://localhost/app/") };

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
