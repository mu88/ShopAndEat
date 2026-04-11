#nullable enable
using FluentAssertions;
using Microsoft.Extensions.AI;
using NUnit.Framework;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolDefinitionProviderTests
{
    private readonly ToolDefinitionProvider _sut = new();

    [Test]
    public void GetToolDefinitions_Returns9Tools()
    {
        // Arrange
        var shopName = "TestShop";

        // Act
        var tools = _sut.GetToolDefinitions(shopName);

        // Assert
        tools.Should().HaveCount(9);
    }

    [Test]
    public void GetToolDefinitions_AllToolsHaveNameAndDescription()
    {
        // Arrange
        var shopName = "TestShop";

        // Act
        var tools = _sut.GetToolDefinitions(shopName);

        // Assert
        foreach (var tool in tools)
        {
            var aiFunction = tool as AIFunction;
            aiFunction.Should().NotBeNull();
            aiFunction!.Name.Should().NotBeNullOrWhiteSpace();
            aiFunction.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [TestCase("search_products")]
    [TestCase("get_product_details")]
    [TestCase("add_to_cart")]
    [TestCase("remove_from_cart")]
    [TestCase("get_cart_contents")]
    [TestCase("navigate_to_cart")]
    [TestCase("save_preference")]
    [TestCase("delete_preference")]
    public void GetToolDefinitions_ContainsTool(string expectedToolName)
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop");

        // Assert
        tools.OfType<AIFunction>().Should().Contain(
            tool => string.Equals(tool.Name, expectedToolName, StringComparison.Ordinal));
    }

    [Test]
    public void GetToolDefinitions_InterpolatesShopNameInDescriptions()
    {
        // Arrange
        var shopName = "SuperMarket";

        // Act
        var tools = _sut.GetToolDefinitions(shopName);

        // Assert
        var shopTools = tools.OfType<AIFunction>()
            .Where(tool => tool.Name is "search_products" or "get_product_details" or "add_to_cart"
                or "remove_from_cart" or "get_cart_contents" or "navigate_to_cart");

        foreach (var tool in shopTools)
        {
            tool.Description.Should().Contain("SuperMarket",
                because: $"tool '{tool.Name}' should include the shop name in its description");
        }
    }

    [Test]
    public void GetToolDefinitions_PreferenceToolsDoNotContainShopName()
    {
        // Arrange
        var shopName = "SuperMarket";

        // Act
        var tools = _sut.GetToolDefinitions(shopName);

        // Assert
        var prefTools = tools.OfType<AIFunction>()
            .Where(tool => tool.Name is "save_preference" or "delete_preference");

        foreach (var tool in prefTools)
        {
            tool.Description.Should().NotContain("SuperMarket");
        }
    }

    [Test]
    public async Task SearchProducts_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var searchTool = FindTool(tools, "search_products");

        // Act
        var result = await searchTool.InvokeAsync(
            new AIFunctionArguments { ["search_term"] = "milk" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task GetProductDetails_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var detailTool = FindTool(tools, "get_product_details");

        // Act
        var result = await detailTool.InvokeAsync(
            new AIFunctionArguments { ["product_url"] = "http://example.com/product" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task AddToCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var cartTool = FindTool(tools, "add_to_cart");

        // Act
        var result = await cartTool.InvokeAsync(
            new AIFunctionArguments { ["product_url"] = "http://example.com/p", ["quantity"] = 2 });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task RemoveFromCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var removeTool = FindTool(tools, "remove_from_cart");

        // Act
        var result = await removeTool.InvokeAsync(
            new AIFunctionArguments { ["product_name"] = "milk" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task GetCartContents_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var cartContentsTool = FindTool(tools, "get_cart_contents");

        // Act
        var result = await cartContentsTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task NavigateToCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var navTool = FindTool(tools, "navigate_to_cart");

        // Act
        var result = await navTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task SavePreference_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var saveTool = FindTool(tools, "save_preference");

        // Act
        var result = await saveTool.InvokeAsync(
            new AIFunctionArguments { ["scope"] = "global", ["key"] = "k", ["value"] = "v" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [Test]
    public async Task DeletePreference_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var deleteTool = FindTool(tools, "delete_preference");

        // Act
        var result = await deleteTool.InvokeAsync(
            new AIFunctionArguments { ["scope"] = "global", ["key"] = "k" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }

    [TestCase("search_products", "search_term")]
    [TestCase("get_product_details", "product_url")]
    [TestCase("remove_from_cart", "product_name")]
    public void GetToolDefinitions_ToolHasExpectedParameter(string toolName, string expectedParam)
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop");
        var tool = FindTool(tools, toolName);

        // Assert
        tool.JsonSchema.ToString().Should().Contain(expectedParam);
    }

    [Test]
    public void GetToolDefinitions_AddToCart_HasQuantityParameter()
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop");
        var tool = FindTool(tools, "add_to_cart");

        // Assert
        var schema = tool.JsonSchema.ToString();
        schema.Should().Contain("product_url");
        schema.Should().Contain("quantity");
    }

    [Test]
    public void GetToolDefinitions_SavePreference_HasAllParameters()
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop");
        var tool = FindTool(tools, "save_preference");

        // Assert
        var schema = tool.JsonSchema.ToString();
        schema.Should().Contain("scope");
        schema.Should().Contain("key");
        schema.Should().Contain("value");
    }

    [Test]
    public void GetToolDefinitions_DeletePreference_HasParameters()
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop");
        var tool = FindTool(tools, "delete_preference");

        // Assert
        var schema = tool.JsonSchema.ToString();
        schema.Should().Contain("scope");
        schema.Should().Contain("key");
    }

    [Test]
    public void GetToolDefinitions_ReturnsNewListOnEachCall()
    {
        // Arrange & Act
        var tools1 = _sut.GetToolDefinitions("Shop1");
        var tools2 = _sut.GetToolDefinitions("Shop2");

        // Assert
        tools1.Should().NotBeSameAs(tools2);
    }

    private static AIFunction FindTool(IReadOnlyList<AITool> tools, string name) =>
        tools.OfType<AIFunction>().Single(
            tool => string.Equals(tool.Name, name, StringComparison.Ordinal));

    [Test]
    public async Task VerifyShoppingList_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop");
        var verifyTool = FindTool(tools, "verify_shopping_list");

        // Act
        var result = await verifyTool.InvokeAsync(
            new AIFunctionArguments { ["shopping_list"] = "1 Packung Milch" });

        // Assert — stub returns no meaningful data
        (result as string).Should().BeNullOrEmpty();
    }
}
