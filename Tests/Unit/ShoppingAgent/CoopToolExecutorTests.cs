using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class CoopToolExecutorTests
{
    private IExtensionBridge _bridgeMock;

    [SetUp]
    public void SetUp()
    {
        _bridgeMock = Substitute.For<IExtensionBridge>();
    }

    [Test]
    public async Task SearchAsync_ParsesJsonResult_WhenBridgeReturnsProducts()
    {
        // Arrange
        var products = new[]
        {
            new ShopProduct { Name = "Organic Tofu", Price = "2.95", Url = "https://coop.ch/p/123" },
            new ShopProduct { Name = "Tofu Natur", Price = "1.80", Url = "https://coop.ch/p/456" },
        };
        var json = JsonSerializer.Serialize(products);
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = json });

        var testee = CreateTestee();

        // Act
        var result = await testee.SearchAsync("Tofu");

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Organic Tofu");
        result[0].Price.Should().Be("2.95");
        result[1].Name.Should().Be("Tofu Natur");
    }

    [Test]
    public async Task SearchAsync_ReturnsEmptyList_WhenBridgeReturnsError()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Extension not connected" });

        var testee = CreateTestee();

        // Act
        var result = await testee.SearchAsync("Tofu");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_PassesSearchTermToBridge()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "[]" });

        var testee = CreateTestee();

        // Act
        await testee.SearchAsync("Cocktailtomaten");

        // Assert
        await _bridgeMock.Received(1).ExecuteToolAsync(
            "search",
            Arg.Is<Dictionary<string, object>>(d => d["term"].ToString() == "Cocktailtomaten"),
            "coop",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddToCartAsync_ReturnsData_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "{\"added\":2,\"requested\":2,\"verified\":true}" });

        var testee = CreateTestee();

        // Act
        var result = await testee.AddToCartAsync("https://coop.ch/p/123", 2);

        // Assert
        result.Should().Contain("added");
    }

    [Test]
    public async Task AddToCartAsync_ReturnsError_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Product unavailable" });

        var testee = CreateTestee();

        // Act
        var result = await testee.AddToCartAsync("https://coop.ch/p/123", 1);

        // Assert
        result.Should().StartWith("ERROR:");
        result.Should().Contain("Product unavailable");
    }

    [Test]
    public async Task GetProductDetailsAsync_ReturnsDetails_WhenBridgeSucceeds()
    {
        // Arrange
        var details = new ProductDetails
        {
            Name = "Organic Tofu Nature",
            Price = "2.95",
            UnitSize = "200g",
            Brand = "Karma",
            IsAvailable = true,
        };
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = JsonSerializer.Serialize(details) });

        var testee = CreateTestee();

        // Act
        var result = await testee.GetProductDetailsAsync("https://coop.ch/p/123");

        // Assert
        result.Name.Should().Be("Organic Tofu Nature");
        result.UnitSize.Should().Be("200g");
        result.Brand.Should().Be("Karma");
    }

    [Test]
    public async Task GetProductDetailsAsync_ReturnsErrorDetails_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Page load failed" });

        var testee = CreateTestee();

        // Act
        var result = await testee.GetProductDetailsAsync("https://coop.ch/p/999");

        // Assert
        result.Name.Should().Be("Error");
        result.Description.Should().Be("Page load failed");
    }

    [Test]
    public async Task RemoveFromCartAsync_ReturnsData_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("removeFromCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "Removed 'Organic Tofu' from cart" });

        var testee = CreateTestee();

        // Act
        var result = await testee.RemoveFromCartAsync("Organic Tofu");

        // Assert
        result.Should().Contain("Removed");
    }

    [Test]
    public async Task RemoveFromCartAsync_ReturnsError_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("removeFromCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Item not in cart" });

        var testee = CreateTestee();

        // Act
        var result = await testee.RemoveFromCartAsync("Tofu");

        // Assert
        result.Should().StartWith("ERROR:");
        result.Should().Contain("Item not in cart");
    }

    [Test]
    public async Task RemoveFromCartAsync_PassesProductNameToBridge()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "ok" });

        var testee = CreateTestee();

        // Act
        await testee.RemoveFromCartAsync("Bio Milch");

        // Assert
        await _bridgeMock.Received(1).ExecuteToolAsync(
            "removeFromCart",
            Arg.Is<Dictionary<string, object>>(d => d["productName"].ToString() == "Bio Milch"),
            "coop",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetCartContentsAsync_ReturnsData_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("getCartContents", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "[{\"name\":\"Tofu\",\"qty\":2}]" });

        var testee = CreateTestee();

        // Act
        var result = await testee.GetCartContentsAsync();

        // Assert
        result.Should().Contain("Tofu");
    }

    [Test]
    public async Task GetCartContentsAsync_ReturnsError_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("getCartContents", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Cart unavailable" });

        var testee = CreateTestee();

        // Act
        var result = await testee.GetCartContentsAsync();

        // Assert
        result.Should().StartWith("ERROR:");
        result.Should().Contain("Cart unavailable");
    }

    [Test]
    public async Task NavigateToCartAsync_ReturnsData_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("navigateToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "Navigated to cart" });

        var testee = CreateTestee();

        // Act
        var result = await testee.NavigateToCartAsync();

        // Assert
        result.Should().Contain("Navigated");
    }

    [Test]
    public async Task NavigateToCartAsync_ReturnsError_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("navigateToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Navigation failed" });

        var testee = CreateTestee();

        // Act
        var result = await testee.NavigateToCartAsync();

        // Assert
        result.Should().StartWith("ERROR:");
        result.Should().Contain("Navigation failed");
    }

    private CoopToolExecutor CreateTestee() => new(_bridgeMock, NullLogger<CoopToolExecutor>.Instance);
}
