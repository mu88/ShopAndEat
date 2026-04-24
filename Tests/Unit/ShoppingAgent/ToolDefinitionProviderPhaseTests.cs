using FluentAssertions;
using Microsoft.Extensions.AI;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolDefinitionProviderPhaseTests
{
    private readonly ToolDefinitionProvider _sut = new();

    private static List<string> ToolNames(IReadOnlyList<AITool> tools) =>
        tools.OfType<AIFunction>().Select(tool => tool.Name).ToList();

    [Test]
    public void GetToolDefinitions_Researching_ContainsConfirmCart()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.Researching));

        // Assert
        names.Should().Contain("confirm_cart");
    }

    [Test]
    public void GetToolDefinitions_Researching_DoesNotContainCartFillingTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.Researching));

        // Assert
        names.Should().NotContain("add_to_cart");
        names.Should().NotContain("navigate_to_cart");
        names.Should().NotContain("remove_from_cart");
        names.Should().NotContain("get_cart_contents");
        names.Should().NotContain("verify_shopping_list");
        names.Should().NotContain("proceed_to_cart");
    }

    [Test]
    public void GetToolDefinitions_AwaitingConfirmation_ContainsConfirmCartAndProceedToCart()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingConfirmation));

        // Assert
        names.Should().Contain("confirm_cart");
        names.Should().Contain("proceed_to_cart");
    }

    [Test]
    public void GetToolDefinitions_AwaitingConfirmation_DoesNotContainCartFillingTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingConfirmation));

        // Assert
        names.Should().NotContain("add_to_cart");
        names.Should().NotContain("navigate_to_cart");
        names.Should().NotContain("remove_from_cart");
        names.Should().NotContain("get_cart_contents");
        names.Should().NotContain("verify_shopping_list");
    }

    [Test]
    public void GetToolDefinitions_FillingCart_ContainsAllCartFillingTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.FillingCart));

        // Assert
        names.Should().Contain("add_to_cart");
        names.Should().Contain("navigate_to_cart");
        names.Should().Contain("get_cart_contents");
        names.Should().Contain("verify_shopping_list");
        names.Should().Contain("remove_from_cart");
    }

    [Test]
    public void GetToolDefinitions_FillingCart_DoesNotContainPhaseTransitionTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.FillingCart));

        // Assert
        names.Should().NotContain("confirm_cart");
        names.Should().NotContain("proceed_to_cart");
    }

    [Test]
    public void GetToolDefinitions_AwaitingClarification_ContainsRequestClarification()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingClarification));

        // Assert
        names.Should().Contain("request_clarification");
    }

    [Test]
    public void GetToolDefinitions_AwaitingClarification_DoesNotContainSearchTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingClarification));

        // Assert
        names.Should().NotContain("search_products");
        names.Should().NotContain("get_product_details");
    }

    [Test]
    public void GetToolDefinitions_AwaitingClarification_DoesNotContainCartFillingTools()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingClarification));

        // Assert
        names.Should().NotContain("add_to_cart");
        names.Should().NotContain("navigate_to_cart");
        names.Should().NotContain("proceed_to_cart");
    }

    [Test]
    public void GetToolDefinitions_AwaitingClarification_ContainsConfirmCartAndPreferences()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.AwaitingClarification));

        // Assert
        names.Should().Contain("confirm_cart");
        names.Should().Contain("save_preference");
        names.Should().Contain("delete_preference");
    }

    [Test]
    public void GetToolDefinitions_Researching_ContainsRequestClarification()
    {
        // Arrange & Act
        var names = ToolNames(_sut.GetToolDefinitions("Shop", WorkflowPhase.Researching));

        // Assert
        names.Should().Contain("request_clarification");
    }

    [Test]
    public void GetToolDefinitions_WithInvalidPhase_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act
        var act = () => _sut.GetToolDefinitions("Shop", (WorkflowPhase)99);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
