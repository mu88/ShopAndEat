using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DataLayer.EfClasses;
using DTO.ShoppingSession;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class SessionsApiTests
{
    private const string BasePath = "shopAndEat/api/shopping/sessions";

    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoSessions()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<List<SessionResponse>>(BasePath);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task Create_ReturnsCreatedSession()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var request = new CreateSessionRequest { IngredientList = "2 Tomatoes\n1 Onion" };

        // Act
        var response = await client.PostAsJsonAsync(BasePath, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("shoppingSessionId").GetInt32().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetById_ReturnsSession_AfterCreate()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "1 Egg" });
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("shoppingSessionId").GetInt32();

        // Act
        var result = await client.GetFromJsonAsync<SessionDetailResponse>($"{BasePath}/{id}");

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(id);
        result.Status.Should().Be("InProgress");
        result.IngredientList.Should().Be("1 Egg");
    }

    [Test]
    public async Task GetById_Returns404_WhenNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{BasePath}/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddItem_ReturnsItem_WhenSessionInProgress()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "2 Tomatoes" });
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("shoppingSessionId").GetInt32();
        var itemRequest = new AddSessionItemRequest
        {
            OriginalIngredient = "2 Tomatoes",
            SelectedProductName = "Organic Tomatoes 500g",
            SelectedProductUrl = "https://coop.ch/product/123",
            Quantity = 1,
            Price = "2.50",
            Status = SessionItemStatus.Added
        };

        // Act
        var itemResponse = await client.PostAsJsonAsync($"{BasePath}/{id}/items", itemRequest);

        // Assert
        itemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var itemBody = await itemResponse.Content.ReadFromJsonAsync<JsonElement>();
        itemBody.GetProperty("shoppingSessionItemId").GetInt32().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task AddItem_Returns400_WhenSessionCompleted()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "1 Apple" });
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("shoppingSessionId").GetInt32();
        await client.PatchAsync($"{BasePath}/{id}/complete", null);
        var itemRequest = new AddSessionItemRequest
        {
            OriginalIngredient = "1 Apple",
            SelectedProductName = "Organic Apple",
            SelectedProductUrl = "https://coop.ch/product/456",
            Quantity = 1,
            Status = SessionItemStatus.Added
        };

        // Act
        var itemResponse = await client.PostAsJsonAsync($"{BasePath}/{id}/items", itemRequest);

        // Assert
        itemResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Complete_UpdatesStatus()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "3 Potatoes" });
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("shoppingSessionId").GetInt32();

        // Act
        var completeResponse = await client.PatchAsync($"{BasePath}/{id}/complete", null);

        // Assert
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var session = await client.GetFromJsonAsync<SessionDetailResponse>($"{BasePath}/{id}");
        session!.Status.Should().Be("Completed");
        session.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Delete_RemovesSession()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "1 Banana" });
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("shoppingSessionId").GetInt32();

        // Act
        var deleteResponse = await client.DeleteAsync($"{BasePath}/{id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"{BasePath}/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_Returns400_WhenIngredientListMissing()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(BasePath, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_Returns404_WhenSessionNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"{BasePath}/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task Complete_Returns404_WhenSessionNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.PatchAsync($"{BasePath}/999/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task AddItem_Returns404_WhenSessionNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var itemRequest = new AddSessionItemRequest
        {
            OriginalIngredient = "2 Tomatoes",
            SelectedProductName = "Organic Tomatoes 500g",
            SelectedProductUrl = "https://coop.ch/product/123",
            Quantity = 1,
            Price = "2.50",
            Status = SessionItemStatus.Added
        };

        // Act
        var response = await client.PostAsJsonAsync($"{BasePath}/999/items", itemRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task GetAll_WithCustomLimit_ReturnsLimitedResults()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "1 Apple" });
        await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "2 Pears" });
        await client.PostAsJsonAsync(BasePath, new CreateSessionRequest { IngredientList = "3 Bananas" });

        // Act
        var result = await client.GetFromJsonAsync<List<SessionResponse>>($"{BasePath}?limit=2");

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
    }
}
