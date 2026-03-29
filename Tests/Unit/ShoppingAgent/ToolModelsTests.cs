using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Models;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolModelsTests
{
    [Test]
    public void ToolRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new ToolRequest();

        // Assert
        request.ToolName.Should().Be(string.Empty);
        request.Arguments.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void ToolRequest_CanSetToolName()
    {
        // Arrange & Act
        var request = new ToolRequest { ToolName = "search_products" };

        // Assert
        request.ToolName.Should().Be("search_products");
    }

    [Test]
    public void ToolRequest_CanSetArguments()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["search_term"] = "milk",
            ["quantity"] = 2,
        };

        // Act
        var request = new ToolRequest { ToolName = "add_to_cart", Arguments = arguments };

        // Assert
        request.Arguments.Should().HaveCount(2);
        request.Arguments["search_term"].Should().Be("milk");
        request.Arguments["quantity"].Should().Be(2);
    }

    [Test]
    public void ToolRequest_EqualityByValue()
    {
        // Arrange
        var args = new Dictionary<string, object> { ["key"] = "value" };
        var first = new ToolRequest { ToolName = "test", Arguments = args };
        var second = new ToolRequest { ToolName = "test", Arguments = args };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void ToolRequest_Inequality_WhenDifferentToolName()
    {
        // Arrange
        var first = new ToolRequest { ToolName = "search_products" };
        var second = new ToolRequest { ToolName = "get_cart_contents" };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Test]
    public void ToolRequest_ToString_ContainsTypeName()
    {
        // Arrange
        var request = new ToolRequest { ToolName = "search_products" };

        // Act
        var result = request.ToString();

        // Assert
        result.Should().Contain("ToolRequest");
    }

    [Test]
    public void ToolRequest_GetHashCode_EqualForEqualValues()
    {
        // Arrange
        var args = new Dictionary<string, object> { ["k"] = "v" };
        var first = new ToolRequest { ToolName = "test", Arguments = args };
        var second = new ToolRequest { ToolName = "test", Arguments = args };

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Test]
    public void ToolResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new ToolResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Id.Should().Be(string.Empty);
        result.Data.Should().Be(string.Empty);
        result.Error.Should().Be(string.Empty);
    }

    [Test]
    public void ToolResult_CanSetSuccessTrue()
    {
        // Arrange & Act
        var result = new ToolResult { Success = true, Id = "call-123", Data = "{\"items\":[]}" };

        // Assert
        result.Success.Should().BeTrue();
        result.Id.Should().Be("call-123");
        result.Data.Should().Be("{\"items\":[]}");
    }

    [Test]
    public void ToolResult_CanSetError()
    {
        // Arrange & Act
        var result = new ToolResult { Success = false, Id = "call-456", Error = "Product not found" };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Test]
    public void ToolResult_EqualityByValue()
    {
        // Arrange
        var first = new ToolResult { Success = true, Id = "id1", Data = "data", Error = string.Empty };
        var second = new ToolResult { Success = true, Id = "id1", Data = "data", Error = string.Empty };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void ToolResult_Inequality_WhenDifferentSuccess()
    {
        // Arrange
        var first = new ToolResult { Success = true, Id = "id1" };
        var second = new ToolResult { Success = false, Id = "id1" };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Test]
    public void ToolResult_ToString_ContainsTypeName()
    {
        // Arrange
        var result = new ToolResult { Success = true, Id = "call-1" };

        // Act
        var text = result.ToString();

        // Assert
        text.Should().Contain("ToolResult");
    }

    [Test]
    public void ToolResult_GetHashCode_EqualForEqualValues()
    {
        // Arrange
        var first = new ToolResult { Success = true, Id = "id1", Data = "d", Error = "e" };
        var second = new ToolResult { Success = true, Id = "id1", Data = "d", Error = "e" };

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Test]
    public void ToolResult_SuccessWithData_RoundTrips()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            Success = true,
            Id = "call-789",
            Data = "[{\"name\":\"Bio Milch\",\"price\":1.99}]",
            Error = string.Empty,
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Id.Should().Be("call-789");
        result.Data.Should().Contain("Bio Milch");
        result.Error.Should().BeEmpty();
    }
}
