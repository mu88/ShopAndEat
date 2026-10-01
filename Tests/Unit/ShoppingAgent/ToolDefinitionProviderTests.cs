#nullable enable
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using NUnit.Framework;
using ShoppingAgent.Models;
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
        var tools = _sut.GetToolDefinitions(shopName, WorkflowPhase.FillingCart);

        // Assert
        tools.Should().HaveCount(9);
    }

    [Test]
    public void GetToolDefinitions_AllToolsHaveNameAndDescription()
    {
        // Arrange
        var shopName = "TestShop";

        // Act
        var tools = _sut.GetToolDefinitions(shopName, WorkflowPhase.FillingCart);

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
        var tools = _sut.GetToolDefinitions("AnyShop", WorkflowPhase.FillingCart);

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
        var tools = _sut.GetToolDefinitions(shopName, WorkflowPhase.FillingCart);

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
        var tools = _sut.GetToolDefinitions(shopName, WorkflowPhase.FillingCart);

        // Assert
        var prefTools = tools.OfType<AIFunction>()
            .Where(tool => tool.Name is "save_preference" or "delete_preference");

        foreach (var tool in prefTools)
        {
            tool.Description.Should().NotContain("SuperMarket");
        }
    }

    [Test]
    public void GetToolDefinitions_DescriptionsMatchExactWording()
    {
        // Arrange
        var expectedDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["search_products"] = "Searches the TestShop online shop for products. Returns a list of products with name, price, and URL.",
            ["get_product_details"] = "Opens a product detail page in the TestShop online shop and returns name, price, unit size, brand, and availability.",
            ["add_to_cart"] = "Adds a product with the specified quantity to the TestShop shopping cart. Uses the shop API directly — reliable and fast. For promotional products, the result contains promoAvailable/promoText.",
            ["remove_from_cart"] = "Removes a product from the TestShop shopping cart. Prefer passing the cart_entry_uid from the add_to_cart result when the product was added in this session — this ensures the correct item is removed. Fall back to product_name (or part of it) only when cart_entry_uid is unavailable.",
            ["get_cart_contents"] = "Returns the current contents of the TestShop shopping cart. Shows name, quantity, price, and product ID for each item.",
            ["navigate_to_cart"] = "Navigates the TestShop tab to the shopping cart in the background. The tab is not brought to the foreground. Use this tool at the end so the user sees the cart when switching tabs.",
            ["save_preference"] = "Saves a learned preference for future shopping. Scope: 'global' for general preferences, 'article:<name>' for article-specific, 'reminder' for reminders. Key: e.g. 'confirmed_product', 'prefer_bio', 'search_term'. Value: the stored value.",
            ["delete_preference"] = "Deletes a saved preference. Use this when the user says 'forget Tofu' (scope='article:Tofu', key='confirmed_product') or 'remove X from the reminder list' (scope='reminder', key='X').",
            ["verify_shopping_list"] = "Verifies that all items from the original shopping list are represented in the current cart. Call this BEFORE navigate_to_cart when processing a shopping list. Pass the original list text exactly as the user provided it. Returns a list of potentially missing items, or 'OK' if everything is accounted for.",
        };

        // Act
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);

        // Assert — exact wording matters because the LLM reads these descriptions to decide tool usage.
        foreach (var (toolName, expectedDescription) in expectedDescriptions)
        {
            FindTool(tools, toolName).Description.Should().Be(expectedDescription, because: $"tool '{toolName}' description drives LLM behavior");
        }
    }

    [Test]
    public void GetToolDefinitions_ResearchingPhaseTools_DescriptionsMatchExactWording()
    {
        // Arrange
        var expectedDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["confirm_cart"] = "Call this tool ONLY when ALL items in the shopping plan table are resolved (no ❓ items remain). This signals the end of the research phase and requests user confirmation. Do NOT call this while any item still shows ❓ — present the table and ask open questions first.",
            ["request_clarification"] = "Call this as the LAST action in a response where you have ALREADY shown the plan table and questions as text. Pass pending_items as comma-separated unresolved item names. NEVER call without text output first. After calling, stop — do NOT search or call confirm_cart until the user replies.",
        };

        // Act
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.Researching);

        // Assert
        foreach (var (toolName, expectedDescription) in expectedDescriptions)
        {
            FindTool(tools, toolName).Description.Should().Be(expectedDescription, because: $"tool '{toolName}' description drives LLM behavior");
        }
    }

    [Test]
    public void GetToolDefinitions_ProceedToCart_DescriptionMatchesExactWording()
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.AwaitingConfirmation);

        // Assert
        FindTool(tools, "proceed_to_cart").Description.Should().Be(
            "Call this tool when the user has explicitly confirmed the shopping plan. This transitions the workflow to the cart-filling phase. After calling this tool, proceed to add all planned products to the cart using add_to_cart.");
    }

    [Test]
    public async Task SearchProducts_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var searchTool = FindTool(tools, "search_products");

        // Act
        var result = await searchTool.InvokeAsync(
            new AIFunctionArguments { ["search_term"] = "milk" });

        // Assert — stub returns no meaningful data. Result is wrapped as JsonElement by AIFunctionFactory.
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task GetProductDetails_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var detailTool = FindTool(tools, "get_product_details");

        // Act
        var result = await detailTool.InvokeAsync(
            new AIFunctionArguments { ["product_url"] = "http://example.com/product" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task AddToCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var cartTool = FindTool(tools, "add_to_cart");

        // Act
        var result = await cartTool.InvokeAsync(
            new AIFunctionArguments { ["product_url"] = "http://example.com/p", ["quantity"] = 2 });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task RemoveFromCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var removeTool = FindTool(tools, "remove_from_cart");

        // Act
        var result = await removeTool.InvokeAsync(
            new AIFunctionArguments { ["product_name"] = "milk" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task GetCartContents_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var cartContentsTool = FindTool(tools, "get_cart_contents");

        // Act
        var result = await cartContentsTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task NavigateToCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var navTool = FindTool(tools, "navigate_to_cart");

        // Act
        var result = await navTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task SavePreference_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var saveTool = FindTool(tools, "save_preference");

        // Act
        var result = await saveTool.InvokeAsync(
            new AIFunctionArguments { ["scope"] = "global", ["key"] = "k", ["value"] = "v" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task DeletePreference_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var deleteTool = FindTool(tools, "delete_preference");

        // Act
        var result = await deleteTool.InvokeAsync(
            new AIFunctionArguments { ["scope"] = "global", ["key"] = "k" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [TestCase("search_products", "search_term")]
    [TestCase("get_product_details", "product_url")]
    [TestCase("remove_from_cart", "product_name")]
    public void GetToolDefinitions_ToolHasExpectedParameter(string toolName, string expectedParam)
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop", WorkflowPhase.FillingCart);
        var tool = FindTool(tools, toolName);

        // Assert
        tool.JsonSchema.ToString().Should().Contain(expectedParam);
    }

    [Test]
    public void GetToolDefinitions_AddToCart_HasQuantityParameter()
    {
        // Arrange & Act
        var tools = _sut.GetToolDefinitions("AnyShop", WorkflowPhase.FillingCart);
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
        var tools = _sut.GetToolDefinitions("AnyShop", WorkflowPhase.FillingCart);
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
        var tools = _sut.GetToolDefinitions("AnyShop", WorkflowPhase.FillingCart);
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
        var tools1 = _sut.GetToolDefinitions("Shop1", WorkflowPhase.FillingCart);
        var tools2 = _sut.GetToolDefinitions("Shop2", WorkflowPhase.FillingCart);

        // Assert
        tools1.Should().NotBeSameAs(tools2);
    }

    private static AIFunction FindTool(IReadOnlyList<AITool> tools, string name) =>
        tools.OfType<AIFunction>().Single(
            tool => string.Equals(tool.Name, name, StringComparison.Ordinal));

    // AIFunctionFactory wraps the stub delegates' return value as a JsonElement rather than a plain string.
    private static string GetStringResult(object? result) =>
        result is JsonElement { ValueKind: JsonValueKind.String } jsonElement ? jsonElement.GetString() ?? string.Empty : string.Empty;

    [Test]
    public async Task VerifyShoppingList_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.FillingCart);
        var verifyTool = FindTool(tools, "verify_shopping_list");

        // Act
        var result = await verifyTool.InvokeAsync(
            new AIFunctionArguments { ["shopping_list"] = "1 Packung Milch" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task ConfirmCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.Researching);
        var confirmTool = FindTool(tools, "confirm_cart");

        // Act
        var result = await confirmTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task RequestClarification_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.Researching);
        var clarificationTool = FindTool(tools, "request_clarification");

        // Act
        var result = await clarificationTool.InvokeAsync(
            new AIFunctionArguments { ["pending_items"] = "milk, bread" });

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }

    [Test]
    public async Task ProceedToCart_FunctionReturnsExpectedResult()
    {
        // Arrange
        var tools = _sut.GetToolDefinitions("TestShop", WorkflowPhase.AwaitingConfirmation);
        var proceedTool = FindTool(tools, "proceed_to_cart");

        // Act
        var result = await proceedTool.InvokeAsync([]);

        // Assert — stub returns no meaningful data
        GetStringResult(result).Should().BeEmpty();
    }
}
