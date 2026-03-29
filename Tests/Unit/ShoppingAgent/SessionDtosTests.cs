using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Services;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class SessionDtosTests
{
    [Test]
    public void SessionSummary_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var summary = new SessionSummary();

        // Assert
        summary.SessionId.Should().Be(0);
        summary.StartedAt.Should().Be(default);
        summary.Status.Should().Be(string.Empty);
        summary.IngredientList.Should().Be(string.Empty);
        summary.ItemCount.Should().Be(0);
    }

    [Test]
    public void SessionSummary_CanSetAllProperties()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        var summary = new SessionSummary
        {
            SessionId = 42,
            StartedAt = startedAt,
            Status = "Completed",
            IngredientList = "Milk, Eggs, Bread",
            ItemCount = 3,
        };

        // Assert
        summary.SessionId.Should().Be(42);
        summary.StartedAt.Should().Be(startedAt);
        summary.Status.Should().Be("Completed");
        summary.IngredientList.Should().Be("Milk, Eggs, Bread");
        summary.ItemCount.Should().Be(3);
    }

    [Test]
    public void SessionSummary_EqualityByValue()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow;
        var first = new SessionSummary { SessionId = 1, StartedAt = startedAt, Status = "Active", IngredientList = "Milk", ItemCount = 1 };
        var second = new SessionSummary { SessionId = 1, StartedAt = startedAt, Status = "Active", IngredientList = "Milk", ItemCount = 1 };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void SessionSummary_Inequality_WhenDifferentValues()
    {
        // Arrange
        var first = new SessionSummary { SessionId = 1 };
        var second = new SessionSummary { SessionId = 2 };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Test]
    public void SessionItemDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var item = new SessionItemDto();

        // Assert
        item.OriginalIngredient.Should().Be(string.Empty);
    }

    [Test]
    public void SessionItemDto_CanSetOriginalIngredient()
    {
        // Arrange & Act
        var item = new SessionItemDto { OriginalIngredient = "3 packs of toast" };

        // Assert
        item.OriginalIngredient.Should().Be("3 packs of toast");
    }

    [Test]
    public void SessionItemDto_EqualityByValue()
    {
        // Arrange
        var first = new SessionItemDto { OriginalIngredient = "Bread" };
        var second = new SessionItemDto { OriginalIngredient = "Bread" };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void IngredientItem_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var item = new IngredientItem();

        // Assert
        item.Text.Should().Be(string.Empty);
        item.Article.Should().Be(string.Empty);
        item.Quantity.Should().Be(0);
        item.Unit.Should().Be(string.Empty);
    }

    [Test]
    public void IngredientItem_CanSetAllProperties()
    {
        // Arrange & Act
        var item = new IngredientItem
        {
            Text = "200g Butter",
            Article = "Butter",
            Quantity = 200.0,
            Unit = "Gram",
        };

        // Assert
        item.Text.Should().Be("200g Butter");
        item.Article.Should().Be("Butter");
        item.Quantity.Should().Be(200.0);
        item.Unit.Should().Be("Gram");
    }

    [Test]
    public void IngredientItem_EqualityByValue()
    {
        // Arrange
        var first = new IngredientItem { Text = "Milk", Article = "Milk", Quantity = 1.0, Unit = "Liter" };
        var second = new IngredientItem { Text = "Milk", Article = "Milk", Quantity = 1.0, Unit = "Liter" };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void IngredientItem_Inequality_WhenDifferentQuantity()
    {
        // Arrange
        var first = new IngredientItem { Quantity = 1.0 };
        var second = new IngredientItem { Quantity = 2.0 };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Test]
    public void SessionSummary_ToString_ContainsTypeName()
    {
        // Arrange
        var summary = new SessionSummary { SessionId = 5, Status = "Active" };

        // Act
        var result = summary.ToString();

        // Assert
        result.Should().Contain("SessionSummary");
    }

    [Test]
    public void SessionItemDto_ToString_ContainsTypeName()
    {
        // Arrange
        var item = new SessionItemDto { OriginalIngredient = "Eggs" };

        // Act
        var result = item.ToString();

        // Assert
        result.Should().Contain("SessionItemDto");
    }

    [Test]
    public void IngredientItem_ToString_ContainsTypeName()
    {
        // Arrange
        var item = new IngredientItem { Article = "Eggs" };

        // Act
        var result = item.ToString();

        // Assert
        result.Should().Contain("IngredientItem");
    }

    [Test]
    public void SessionSummary_GetHashCode_EqualForEqualValues()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow;
        var first = new SessionSummary { SessionId = 1, StartedAt = startedAt, Status = "Active", IngredientList = "X", ItemCount = 1 };
        var second = new SessionSummary { SessionId = 1, StartedAt = startedAt, Status = "Active", IngredientList = "X", ItemCount = 1 };

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
