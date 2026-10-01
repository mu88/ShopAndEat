#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolCallDispatcherTests
{
    private static readonly string[] GarlicLemonItems = ["Garlic", "Lemon"];

    private IShopToolExecutorFactory _factoryMock = null!;
    private IShopToolExecutor _executorMock = null!;
    private IPreferencesService _preferencesMock = null!;
    private IShoppingListVerifier _verifierMock = null!;
    private IStringLocalizer<Messages> _localizerMock = null!;
    private IShoppingWorkflowState _workflowStateMock = null!;
    private ToolCallDispatcher _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factoryMock = Substitute.For<IShopToolExecutorFactory>();
        _executorMock = Substitute.For<IShopToolExecutor>();
        _factoryMock.GetExecutor(Arg.Any<string>()).Returns(_executorMock);

        _preferencesMock = Substitute.For<IPreferencesService>();

        _verifierMock = Substitute.For<IShoppingListVerifier>();

        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        _localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));

        _workflowStateMock = Substitute.For<IShoppingWorkflowState>();

        _sut = new ToolCallDispatcher(_factoryMock, _preferencesMock, _verifierMock, _localizerMock, _workflowStateMock);
    }

    [Test]
    public async Task DispatchAsync_SearchProducts_CallsSearchAsync()
    {
        // Arrange
        _executorMock.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var toolCall = CreateToolCall("search_products", new Dictionary<string, object?> { ["search_term"] = "milk" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        await _executorMock.Received(1).SearchAsync("milk", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_GetProductDetails_CallsGetProductDetailsAsync()
    {
        // Arrange
        _executorMock.GetProductDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult(new global::ShoppingAgent.Models.ProductDetails()));
        var toolCall = CreateToolCall("get_product_details", new Dictionary<string, object?> { ["product_url"] = "https://example.com/product" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        await _executorMock.Received(1).GetProductDetailsAsync("https://example.com/product", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_AddToCart_CallsAddToCartAsync()
    {
        // Arrange
        _executorMock.AddToCartAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Added");
        var toolCall = CreateToolCall("add_to_cart", new Dictionary<string, object?> { ["product_url"] = "https://example.com/product", ["quantity"] = "2" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Added");
        await _executorMock.Received(1).AddToCartAsync("https://example.com/product", 2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_AddToCart_DefaultsQuantityTo1_WhenInvalid()
    {
        // Arrange
        _executorMock.AddToCartAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Added");
        var toolCall = CreateToolCall("add_to_cart", new Dictionary<string, object?> { ["product_url"] = "https://example.com/product", ["quantity"] = "invalid" });

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        await _executorMock.Received(1).AddToCartAsync("https://example.com/product", 1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_RemoveFromCart_CallsRemoveFromCartAsync()
    {
        // Arrange
        _executorMock.RemoveFromCartAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Removed");
        var toolCall = CreateToolCall("remove_from_cart", new Dictionary<string, object?> { ["product_name"] = "Milk" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Removed");
        await _executorMock.Received(1).RemoveFromCartAsync("Milk", string.Empty, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_RemoveFromCart_WithNullCartEntryUid_PassesEmptyStringToExecutor()
    {
        // Arrange
        _executorMock.RemoveFromCartAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Removed");
        var toolCall = CreateToolCall("remove_from_cart", new Dictionary<string, object?>
        {
            ["product_name"] = "Milk",
            ["cart_entry_uid"] = null,
        });

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert — key present but with a null value must fall back to string.Empty, same as a missing key.
        await _executorMock.Received(1).RemoveFromCartAsync("Milk", string.Empty, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_RemoveFromCart_WithCartEntryUid_PassesUidToExecutor()
    {
        // Arrange
        _executorMock.RemoveFromCartAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Removed");
        var toolCall = CreateToolCall("remove_from_cart", new Dictionary<string, object?>
        {
            ["product_name"] = "Milk",
            ["cart_entry_uid"] = "uid-123",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        await _executorMock.Received(1).RemoveFromCartAsync("Milk", "uid-123", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_GetCartContents_CallsGetCartContentsAsync()
    {
        // Arrange
        _executorMock.GetCartContentsAsync(Arg.Any<CancellationToken>())
            .Returns("Cart contents");
        var toolCall = CreateToolCall("get_cart_contents", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Cart contents");
    }

    [Test]
    public async Task DispatchAsync_NavigateToCart_CallsNavigateToCartAsync()
    {
        // Arrange
        _executorMock.NavigateToCartAsync(Arg.Any<CancellationToken>())
            .Returns("Navigated");
        var toolCall = CreateToolCall("navigate_to_cart", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Navigated");
    }

    [Test]
    public async Task DispatchAsync_RequestClarification_WithoutPendingItems_TreatsAsNoItems()
    {
        // Arrange — no "pending_items" argument at all, so GetArg returns string.Empty, which is
        // whitespace-only. This must take the "no items" branch (empty array) instead of falling
        // through to Split(), which would also produce an empty array from an empty string, but
        // isolates the ternary's guard condition rather than relying on Split's own robustness.
        var toolCall = CreateToolCall("request_clarification", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Contain("the items above");
        _workflowStateMock.Received(1).MoveToAwaitingClarification(Arg.Is<IEnumerable<string>>(items => !items.Any()));
    }

    [Test]
    public async Task DispatchAsync_RequestClarification_WithPendingItems_SplitsCommaSeparatedList()
    {
        // Arrange
        var toolCall = CreateToolCall("request_clarification", new Dictionary<string, object?>
        {
            ["pending_items"] = "Garlic, Lemon",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Contain("Garlic, Lemon");
        _workflowStateMock.Received(1).MoveToAwaitingClarification(Arg.Is<IEnumerable<string>>(items => items.SequenceEqual(GarlicLemonItems)));
    }

    [Test]
    public async Task DispatchAsync_SavePreference_CallsPreferencesService()
    {
        // Arrange
        var toolCall = CreateToolCall("save_preference", new Dictionary<string, object?>
        {
            ["scope"] = "global",
            ["key"] = "prefer_bio",
            ["value"] = "true",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("PreferenceSaved");
        await _preferencesMock.Received(1).SavePreferenceAsync(
            Arg.Is<PreferenceDto>(p => p.Scope == "global" && p.Key == "prefer_bio" && p.Value == "true" && p.StoreKey == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_SavePreference_MissingScope_DefaultsToGlobal()
    {
        // Arrange
        var toolCall = CreateToolCall("save_preference", new Dictionary<string, object?>
        {
            ["key"] = "prefer_bio",
            ["value"] = "true",
        });

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        await _preferencesMock.Received(1).SavePreferenceAsync(
            Arg.Is<PreferenceDto>(p => p.Scope == "global"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_SavePreference_ArticleScope_SetsStoreKey()
    {
        // Arrange
        var toolCall = CreateToolCall("save_preference", new Dictionary<string, object?>
        {
            ["scope"] = "article:Tofu",
            ["key"] = "confirmed_product",
            ["value"] = "https://example.com/tofu",
        });

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        await _preferencesMock.Received(1).SavePreferenceAsync(
            Arg.Is<PreferenceDto>(p => p.StoreKey == "coop"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_DeletePreference_CallsPreferencesService()
    {
        // Arrange
        _preferencesMock.DeletePreferenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var toolCall = CreateToolCall("delete_preference", new Dictionary<string, object?>
        {
            ["scope"] = "global",
            ["key"] = "prefer_bio",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("PreferenceDeleted");
        await _preferencesMock.Received(1).DeletePreferenceAsync("global", "prefer_bio", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_DeletePreference_MissingScope_DefaultsToGlobal()
    {
        // Arrange
        _preferencesMock.DeletePreferenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var toolCall = CreateToolCall("delete_preference", new Dictionary<string, object?>
        {
            ["key"] = "prefer_bio",
        });

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        await _preferencesMock.Received(1).DeletePreferenceAsync("global", "prefer_bio", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_DeletePreference_ReturnsNotFound_WhenDeleteFails()
    {
        // Arrange
        _preferencesMock.DeletePreferenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var toolCall = CreateToolCall("delete_preference", new Dictionary<string, object?>
        {
            ["scope"] = "global",
            ["key"] = "unknown_key",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("PreferenceNotFound");
    }

    [Test]
    public async Task DispatchAsync_UnknownTool_ReturnsErrorResult()
    {
        // Arrange
        var toolCall = CreateToolCall("unknown_tool", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("UnknownTool");
    }

    [Test]
    public async Task DispatchAsync_Exception_ReturnsErrorTuple()
    {
        // Arrange
        _executorMock.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<global::ShoppingAgent.Models.ShopProduct>>(_ => throw new InvalidOperationException("Something broke"));
        var toolCall = CreateToolCall("search_products", new Dictionary<string, object?> { ["search_term"] = "milk" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeFalse();
        result.Should().Be("ToolError");
    }

    [Test]
    public async Task DispatchAsync_OperationCanceled_Rethrows()
    {
        // Arrange
        _executorMock.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<global::ShoppingAgent.Models.ShopProduct>>(_ => throw new OperationCanceledException());
        var toolCall = CreateToolCall("search_products", new Dictionary<string, object?> { ["search_term"] = "milk" });

        // Act
        var act = () => _sut.DispatchAsync(toolCall, "coop");

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void GroupConsecutiveToolCalls_GroupsSameTypeTools()
    {
        // Arrange
        var toolCalls = new List<FunctionCallContent>
        {
            CreateToolCall("search_products", new Dictionary<string, object?> { ["search_term"] = "milk" }),
            CreateToolCall("get_product_details", new Dictionary<string, object?> { ["product_url"] = "url1" }),
        };

        // Act
        var groups = _sut.GroupConsecutiveToolCalls(toolCalls);

        // Assert
        groups.Should().HaveCount(1);
        groups[0].Key.Should().Be("search");
        groups[0].Tools.Should().HaveCount(2);
    }

    [Test]
    public void GroupConsecutiveToolCalls_SplitsDifferentToolTypes()
    {
        // Arrange
        var toolCalls = new List<FunctionCallContent>
        {
            CreateToolCall("search_products", new Dictionary<string, object?> { ["search_term"] = "milk" }),
            CreateToolCall("add_to_cart", new Dictionary<string, object?> { ["product_url"] = "url1", ["quantity"] = "1" }),
            CreateToolCall("save_preference", new Dictionary<string, object?> { ["scope"] = "global", ["key"] = "k", ["value"] = "v" }),
        };

        // Act
        var groups = _sut.GroupConsecutiveToolCalls(toolCalls);

        // Assert
        groups.Should().HaveCount(3);
        groups[0].Key.Should().Be("search");
        groups[1].Key.Should().Be("cart");
        groups[2].Key.Should().Be("prefs");
    }

    [Test]
    public void GroupConsecutiveToolCalls_MapsClarificationAndWorkflowToolsToTheirOwnGroups()
    {
        // Arrange
        var toolCalls = new List<FunctionCallContent>
        {
            CreateToolCall("request_clarification", []),
            CreateToolCall("confirm_cart", []),
            CreateToolCall("proceed_to_cart", []),
        };

        // Act
        var groups = _sut.GroupConsecutiveToolCalls(toolCalls);

        // Assert
        groups.Should().HaveCount(2);
        groups[0].Key.Should().Be("clarify");
        groups[0].Label.Should().Be("Clarification Needed");
        groups[0].Icon.Should().Be("❓");
        groups[1].Key.Should().Be("workflow");
        groups[1].Label.Should().Be("Shopping Plan");
        groups[1].Icon.Should().Be("📋");
        groups[1].Tools.Should().HaveCount(2);
    }

    [Test]
    public void GroupConsecutiveToolCalls_MapsProceedToCartToWorkflowGroup()
    {
        // Arrange — isolated from confirm_cart so its own "workflow"/"Shopping Plan"/"📋" literals are
        // the only source for this group's values (a shared group with confirm_cart wouldn't
        // distinguish which of the two identical case bodies actually produced them).
        var toolCalls = new List<FunctionCallContent> { CreateToolCall("proceed_to_cart", []) };

        // Act
        var groups = _sut.GroupConsecutiveToolCalls(toolCalls);

        // Assert
        groups.Should().ContainSingle();
        groups[0].Key.Should().Be("workflow");
        groups[0].Label.Should().Be("Shopping Plan");
        groups[0].Icon.Should().Be("📋");
    }

    [Test]
    public void FormatArgs_EmptyArgs_ReturnsEmptyString()
    {
        // Arrange
        var args = new Dictionary<string, object?>();

        // Act
        var result = _sut.FormatArgs(args);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FormatArgs_NullArgs_ReturnsEmptyString()
    {
        // Arrange & Act
        var result = _sut.FormatArgs(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FormatArgs_MultipleArgs_ReturnsFormattedString()
    {
        // Arrange
        var args = new Dictionary<string, object?> { ["key1"] = "value1", ["key2"] = "value2" };

        // Act
        var result = _sut.FormatArgs(args);

        // Assert
        result.Should().Contain("key1=value1");
        result.Should().Contain("key2=value2");
        result.Should().Contain(", ");
    }

    [TestCase("search_products", "search", "Product Search", "🔍")]
    [TestCase("get_product_details", "search", "Product Search", "🔍")]
    [TestCase("add_to_cart", "cart", "Shopping Cart", "🛒")]
    [TestCase("remove_from_cart", "cart", "Shopping Cart", "🛒")]
    [TestCase("get_cart_contents", "cart", "Shopping Cart", "🛒")]
    [TestCase("navigate_to_cart", "cart", "Shopping Cart", "🛒")]
    [TestCase("save_preference", "prefs", "Preferences", "💾")]
    [TestCase("delete_preference", "prefs", "Preferences", "💾")]
    [TestCase("get_preferences", "prefs", "Preferences", "💾")]
    [TestCase("verify_shopping_list", "verify", "Cart Verification", "✅")]
    [TestCase("completely_unknown", "other", "Processing", "🔧")]
    public void GetToolGroup_ReturnsCorrectGroup(string toolName, string expectedKey, string expectedLabel, string expectedIcon)
    {
        // Arrange & Act
        var (key, label, icon) = IToolCallDispatcher.GetToolGroup(toolName);

        // Assert
        key.Should().Be(expectedKey);
        label.Should().Be(expectedLabel);
        icon.Should().Be(expectedIcon);
    }

    private static FunctionCallContent CreateToolCall(string name, Dictionary<string, object?> args) =>
        new("call-id", name, args);

    [Test]
    public async Task DispatchAsync_NavigateToCart_ResetsWorkflowState()
    {
        // Arrange
        _executorMock.NavigateToCartAsync(Arg.Any<CancellationToken>()).Returns("Navigated");
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var toolCall = CreateToolCall("navigate_to_cart", []);

        // Act
        await _sut.DispatchAsync(toolCall, "coop");

        // Assert — after cart navigation the shopping session ends; state must be reset for the next run
        _workflowStateMock.Received(1).Reset();
    }

    [Test]
    public async Task DispatchAsync_NavigateToCart_WithReminders_AppendsReminderGate()
    {
        // Arrange
        _executorMock.NavigateToCartAsync(Arg.Any<CancellationToken>()).Returns("Navigated");
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new() { Scope = "reminder", Key = "Kaffee", Value = "true" },
                new() { Scope = "reminder", Key = "Bier", Value = "true" },
            ]);
        // Make the localizer echo the joined reminder list back, so the exact join separator is observable.
        _localizerMock["ReminderGate", Arg.Any<object[]>()].Returns(call =>
            new LocalizedString("ReminderGate", $"ReminderGate:{call.ArgAt<object[]>(1)[0]}"));
        var toolCall = CreateToolCall("navigate_to_cart", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Navigated" + Environment.NewLine + "ReminderGate:Kaffee, Bier");
        await _preferencesMock.Received(1).GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_VerifyShoppingList_AllItemsFound_ReturnsOk()
    {
        // Arrange
        _executorMock.GetCartContentsAsync(Arg.Any<CancellationToken>()).Returns("[{\"Name\":\"Milch\"}]");
        _verifierMock.FindMissingItems(Arg.Any<string>(), Arg.Any<string>()).Returns([]);
        var toolCall = CreateToolCall("verify_shopping_list", new Dictionary<string, object?>
        {
            ["shopping_list"] = "1 Packung Milch",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("OK — all items from the shopping list appear to be in the cart.");
        _verifierMock.Received(1).FindMissingItems("1 Packung Milch", "[{\"Name\":\"Milch\"}]");
    }

    [Test]
    public async Task DispatchAsync_VerifyShoppingList_MissingItems_ReturnsMissingList()
    {
        // Arrange
        _executorMock.GetCartContentsAsync(Arg.Any<CancellationToken>()).Returns("[{\"Name\":\"Milch\"}]");
        _verifierMock.FindMissingItems(Arg.Any<string>(), Arg.Any<string>()).Returns(["Lauch", "Kartoffeln"]);
        var toolCall = CreateToolCall("verify_shopping_list", new Dictionary<string, object?>
        {
            ["shopping_list"] = "1 Stück Lauch\n2 Stück Kartoffeln\n1 Packung Milch",
        });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be("Potentially missing from cart: Lauch, Kartoffeln. Please check and add them before navigating to cart.");
        _verifierMock.Received(1).FindMissingItems(
            "1 Stück Lauch\n2 Stück Kartoffeln\n1 Packung Milch",
            "[{\"Name\":\"Milch\"}]");
    }

    [Test]
    public async Task ConfirmCart_ShouldMoveToAwaitingConfirmationAndReturnSentinel()
    {
        // Arrange
        var toolCall = CreateToolCall("confirm_cart", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().StartWith("__phase:");
        _workflowStateMock.Received(1).MoveToAwaitingConfirmation();
    }

    [Test]
    public async Task ProceedToCart_ShouldMoveToFillingCartAndReturnSentinel()
    {
        // Arrange
        var toolCall = CreateToolCall("proceed_to_cart", []);

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().StartWith("__phase:");
        _workflowStateMock.Received(1).MoveToFillingCart();
    }

    [Test]
    public void Phase_ReturnsWorkflowStatePhase()
    {
        // Arrange
        _workflowStateMock.Phase.Returns(WorkflowPhase.FillingCart);

        // Act
        var phase = _sut.Phase;

        // Assert
        phase.Should().Be(WorkflowPhase.FillingCart);
    }

    [Test]
    public void ResetWorkflow_CallsWorkflowStateReset()
    {
        // Arrange & Act
        _sut.ResetWorkflow();

        // Assert
        _workflowStateMock.Received(1).Reset();
    }

    [Test]
    public void ShouldBreakAfterToolExecution_WhenPhaseIsAwaitingConfirmation_ReturnsTrue()
    {
        // Arrange
        _workflowStateMock.Phase.Returns(WorkflowPhase.AwaitingConfirmation);

        // Act & Assert
        _sut.ShouldBreakAfterToolExecution.Should().BeTrue();
    }

    [Test]
    public void ShouldBreakAfterToolExecution_WhenPhaseIsResearching_ReturnsFalse()
    {
        // Arrange
        _workflowStateMock.Phase.Returns(WorkflowPhase.Researching);

        // Act & Assert
        _sut.ShouldBreakAfterToolExecution.Should().BeFalse();
    }

    [Test]
    public void ShouldBreakAfterToolExecution_WhenPhaseIsAwaitingClarification_ReturnsTrue()
    {
        // Arrange
        _workflowStateMock.Phase.Returns(WorkflowPhase.AwaitingClarification);

        // Act & Assert
        _sut.ShouldBreakAfterToolExecution.Should().BeTrue();
    }

    [Test]
    public async Task DispatchAsync_RequestClarification_TransitionsToAwaitingClarificationAndReturnsInstruction()
    {
        // Arrange
        var toolCall = new FunctionCallContent(
            "call_clarify",
            "request_clarification",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["pending_items"] = "Garlic, Lemon" });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Be(
            "AWAITING CLARIFICATION. Unresolved items: Garlic, Lemon. " +
            "INSTRUCTION: Do NOT search for any products NOW. Do NOT call confirm_cart NOW. " +
            "Wait for the user to reply. " +
            "When the user replies: " +
            "(1) Search for every product the user names by calling search_products. " +
            "(2) Output the COMPLETE updated plan table as text — ALL rows, not just changed ones. " +
            "(3) Below the table, ask about ALL still-open ❓ items in text. " +
            "(4) Only THEN call request_clarification for remaining ❓ items. " +
            "You MUST output the table and questions as text before calling request_clarification. " +
            "NEVER skip the table output, even if most rows are unchanged.");
        _workflowStateMock.Received(1).MoveToAwaitingClarification(
            Arg.Is<IEnumerable<string>>(items => items.SequenceEqual(new[] { "Garlic", "Lemon" })));
    }

    [Test]
    public async Task DispatchAsync_RequestClarification_WithEmptyPendingItems_UsesGenericFallback()
    {
        // Arrange
        var toolCall = new FunctionCallContent(
            "call_clarify",
            "request_clarification",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["pending_items"] = "   " });

        // Act
        var (result, success) = await _sut.DispatchAsync(toolCall, "coop");

        // Assert
        success.Should().BeTrue();
        result.Should().Contain("the items above");
        _workflowStateMock.Received(1).MoveToAwaitingClarification(
            Arg.Is<IEnumerable<string>>(items => !items.Any()));
    }
}