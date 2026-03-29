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
public class SessionServiceTests
{
    [Test]
    public async Task CreateSessionAsync_ReturnsSessionId_OnSuccess()
    {
        // Arrange
        var body = JsonSerializer.Serialize(new { ShoppingSessionId = 42 });
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.CreateSessionAsync("3 packs of toast");

        // Assert
        result.Should().Be(42);
    }

    [Test]
    public async Task CreateSessionAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var act = async () => await testee.CreateSessionAsync("ingredients");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetUnitsAsync_ReturnsUnits_OnSuccess()
    {
        // Arrange
        var body = JsonSerializer.Serialize(new[] { "Pack", "Bunch", "Gram" });
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetUnitsAsync();

        // Assert
        result.Should().BeEquivalentTo(new[] { "Pack", "Bunch", "Gram" });
    }

    [Test]
    public async Task GetUnitsAsync_ReturnsEmptyList_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetUnitsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetSessionsAsync_ReturnsEmptyList_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetSessionsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetIngredientListAsync_ReturnsItems_OnSuccess()
    {
        // Arrange
        var body = JsonSerializer.Serialize(new
        {
            Items = new[]
            {
                new { Text = "3 packs of toast", Article = "Toast", Quantity = 3.0, Unit = "Pack" },
            },
        });
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetIngredientListAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Article.Should().Be("Toast");
        result[0].Unit.Should().Be("Pack");
    }

    [Test]
    public async Task GetIngredientListAsync_ReturnsEmptyList_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetIngredientListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetIngredientListAsync_ReturnsEmptyList_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetIngredientListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetSessionsAsync_ReturnsSessions_OnSuccess()
    {
        // Arrange
        var sessions = new[]
        {
            new { SessionId = 1, StartedAt = DateTimeOffset.UtcNow, Status = "Completed", IngredientList = "Milk, Eggs", ItemCount = 2 },
            new { SessionId = 2, StartedAt = DateTimeOffset.UtcNow, Status = "Active", IngredientList = "Bread", ItemCount = 1 },
        };
        var body = JsonSerializer.Serialize(sessions);
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetSessionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].SessionId.Should().Be(1);
        result[1].Status.Should().Be("Active");
    }

    [Test]
    public async Task GetSessionsAsync_IncludesLimitInQueryString()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        await testee.GetSessionsAsync(limit: 5);

        // Assert
        handler.LastRequest.RequestUri!.ToString().Should().Contain("limit=5");
    }

    [Test]
    public async Task GetSessionsAsync_ReturnsEmptyList_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetSessionsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task AddSessionItemAsync_PostsItemToCorrectUrl()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);
        var item = new SessionItemDto { OriginalIngredient = "3 packs of toast" };

        // Act
        await testee.AddSessionItemAsync(42, item);

        // Assert
        handler.LastRequest.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Contain("api/shopping/sessions/42/items");
    }

    [Test]
    public async Task AddSessionItemAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);
        var item = new SessionItemDto { OriginalIngredient = "toast" };

        // Act
        var act = async () => await testee.AddSessionItemAsync(42, item);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task CompleteSessionAsync_SendsPatchToCorrectUrl()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        await testee.CompleteSessionAsync(42);

        // Assert
        handler.LastRequest.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().Should().Contain("api/shopping/sessions/42/complete");
    }

    [Test]
    public async Task CompleteSessionAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var act = async () => await testee.CompleteSessionAsync(42);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task CreateSessionAsync_ReturnsZero_WhenResponseBodyIsNull()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.CreateSessionAsync("ingredients");

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public async Task CreateSessionAsync_PostsToCorrectUrl()
    {
        // Arrange
        var body = JsonSerializer.Serialize(new { ShoppingSessionId = 10 });
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        await testee.CreateSessionAsync("Milk, Eggs");

        // Assert
        handler.LastRequest.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Contain("api/shopping/sessions");
    }

    [Test]
    public async Task GetUnitsAsync_ReturnsEmptyList_WhenExceptionThrown()
    {
        // Arrange
        var handler = new MockHttpHandler(_ => throw new HttpRequestException("network error"));

        var testee = new SessionService(CreateClient(handler), NullLogger<SessionService>.Instance);

        // Act
        var result = await testee.GetUnitsAsync();

        // Assert
        result.Should().BeEmpty();
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
