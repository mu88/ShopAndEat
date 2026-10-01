using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Models;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class CoopToolExecutorTests
{
    private readonly List<Activity> _completedActivities = [];
    private IExtensionBridge _bridgeMock = null!;
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _bridgeMock = Substitute.For<IExtensionBridge>();

        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ShoppingAgentDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

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

    [Test]
    public async Task RemoveFromCartAsync_WithCartEntryUid_PassesUidToBridge()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "Removed" });

        var testee = CreateTestee();

        // Act
        await testee.RemoveFromCartAsync("Bio Milch", "uid-abc123");

        // Assert
        await _bridgeMock.Received(1).ExecuteToolAsync(
            "removeFromCart",
            Arg.Is<Dictionary<string, object>>(d =>
                d["productName"].ToString() == "Bio Milch" &&
                d["cartEntryUid"].ToString() == "uid-abc123"),
            "coop",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchAsync_DeserializesCaseInsensitively_WhenJsonKeysUseDifferentCasing()
    {
        // Arrange
        const string json = """[{"NAME":"Organic Tofu","PRICE":"2.95","URL":"https://coop.ch/p/123"}]""";
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = json });

        var testee = CreateTestee();

        // Act
        var result = await testee.SearchAsync("Tofu");

        // Assert
        result.Should().ContainSingle().Which.Name.Should().Be("Organic Tofu");
    }

    [Test]
    public async Task SearchAsync_ReturnsEmptyList_WhenBridgeReturnsJsonNull()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "null" });

        var testee = CreateTestee();

        // Act
        var result = await testee.SearchAsync("Tofu");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task SearchAsync_RecordsActivityTagsAndName_WhenBridgeSucceeds()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new[] { new ShopProduct { Name = "Tofu" } });
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = json });

        var testee = CreateTestee();

        // Act
        await testee.SearchAsync("Tofu");

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.Coop.SearchProducts");
        activity.GetTagItem("coop.search_term").Should().Be("Tofu");
        activity.GetTagItem("coop.result_count").Should().Be(1);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task SearchAsync_SetsErrorStatusOnActivity_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Extension not connected" });

        var testee = CreateTestee();

        // Act
        await testee.SearchAsync("Tofu");

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Extension not connected");
    }

    [Test]
    public async Task GetProductDetailsAsync_PassesUrlToBridge()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new ProductDetails()) });

        var testee = CreateTestee();

        // Act
        await testee.GetProductDetailsAsync("https://coop.ch/p/123");

        // Assert
        await _bridgeMock.Received(1).ExecuteToolAsync(
            "getProductDetails",
            Arg.Is<Dictionary<string, object>>(d => d["url"].ToString() == "https://coop.ch/p/123"),
            "coop",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetProductDetailsAsync_RecordsActivityTagsAndName_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new ProductDetails { Name = "Tofu" }) });

        var testee = CreateTestee();

        // Act
        await testee.GetProductDetailsAsync("https://coop.ch/p/123");

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.Coop.GetProductDetails");
        activity.GetTagItem("coop.product_url").Should().Be("https://coop.ch/p/123");
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task GetProductDetailsAsync_SetsErrorStatusOnActivity_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Page load failed" });

        var testee = CreateTestee();

        // Act
        await testee.GetProductDetailsAsync("https://coop.ch/p/999");

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Page load failed");
    }

    [Test]
    public async Task AddToCartAsync_PassesUrlAndQuantityToBridge()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "ok" });

        var testee = CreateTestee();

        // Act
        await testee.AddToCartAsync("https://coop.ch/p/123", 3);

        // Assert
        await _bridgeMock.Received(1).ExecuteToolAsync(
            "addToCart",
            Arg.Is<Dictionary<string, object>>(d =>
                d["url"].ToString() == "https://coop.ch/p/123" &&
                d["quantity"].Equals(3)),
            "coop",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddToCartAsync_RecordsActivityTagsAndName_WhenBridgeSucceeds()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "ok" });

        var testee = CreateTestee();

        // Act
        await testee.AddToCartAsync("https://coop.ch/p/123", 3);

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.Coop.AddToCart");
        activity.GetTagItem("coop.product_url").Should().Be("https://coop.ch/p/123");
        activity.GetTagItem("coop.quantity").Should().Be(3);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task AddToCartAsync_SetsErrorStatusOnActivity_WhenBridgeFails()
    {
        // Arrange
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "Product unavailable" });

        var testee = CreateTestee();

        // Act
        await testee.AddToCartAsync("https://coop.ch/p/123", 1);

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Product unavailable");
    }

    [Test]
    public async Task SearchAsync_GetProductDetailsAsync_AddToCartAsync_DoNotThrow_WhenNoActivityListenerIsRegistered_AndCallsSucceed()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetTag` branches used for optional OpenTelemetry tagging.
        _activityListener.Dispose();
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "[]" });
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new ProductDetails()) });
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = true, Data = "ok" });

        var testee = CreateTestee();

        // Act
        var search = async () => await testee.SearchAsync("Tofu");
        var details = async () => await testee.GetProductDetailsAsync("https://coop.ch/p/1");
        var addToCart = async () => await testee.AddToCartAsync("https://coop.ch/p/1", 1);

        // Assert
        await search.Should().NotThrowAsync();
        await details.Should().NotThrowAsync();
        await addToCart.Should().NotThrowAsync();
    }

    [Test]
    public async Task SearchAsync_GetProductDetailsAsync_AddToCartAsync_DoNotThrow_WhenNoActivityListenerIsRegistered_AndCallsFail()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branches used for optional OpenTelemetry error tagging.
        _activityListener.Dispose();
        _bridgeMock
            .ExecuteToolAsync("search", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "boom" });
        _bridgeMock
            .ExecuteToolAsync("getProductDetails", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "boom" });
        _bridgeMock
            .ExecuteToolAsync("addToCart", Arg.Any<Dictionary<string, object>>(), "coop", Arg.Any<CancellationToken>())
            .Returns(new ToolResult { Success = false, Error = "boom" });

        var testee = CreateTestee();

        // Act
        var search = async () => await testee.SearchAsync("Tofu");
        var details = async () => await testee.GetProductDetailsAsync("https://coop.ch/p/1");
        var addToCart = async () => await testee.AddToCartAsync("https://coop.ch/p/1", 1);

        // Assert
        await search.Should().NotThrowAsync();
        await details.Should().NotThrowAsync();
        await addToCart.Should().NotThrowAsync();
    }

    private CoopToolExecutor CreateTestee() => new(_bridgeMock, NullLogger<CoopToolExecutor>.Instance);
}
