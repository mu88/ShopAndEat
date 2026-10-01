using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ShoppingSessionItemTests
{
    [Test]
    public void CreateShoppingSessionItem()
    {
        // Arrange
        var originalIngredient = "500g carrots";
        var sessionId = new ShoppingSessionId(1);
        var addedAt = DateTimeOffset.UtcNow;

        // Act
        var testee = new ShoppingSessionItem(originalIngredient, sessionId, addedAt);

        // Assert
        testee.OriginalIngredient.Should().Be(originalIngredient);
        testee.SessionId.Should().Be(sessionId);
        testee.AddedAt.Should().Be(addedAt);
        testee.SelectedProductName.Should().Be(string.Empty);
        testee.SelectedProductUrl.Should().Be(string.Empty);
        testee.Quantity.Should().Be(1);
        testee.Price.Should().Be(string.Empty);
        testee.Status.Should().Be(SessionItemStatus.Added);
    }

    [Test]
    public void CreateShoppingSessionItem_WithAllOptionalValues_SetsThemAllViaConstructor()
    {
        // Arrange — no update use case exists after construction, so every field is a constructor parameter.
        var originalIngredient = "500g carrots";
        var sessionId = new ShoppingSessionId(1);
        var addedAt = DateTimeOffset.UtcNow;

        // Act
        var testee = new ShoppingSessionItem(
            originalIngredient,
            sessionId,
            addedAt,
            "Bio Karotten 500g",
            "https://coop.ch/p/1",
            2,
            "1.95",
            SessionItemStatus.Skipped);

        // Assert
        testee.SelectedProductName.Should().Be("Bio Karotten 500g");
        testee.SelectedProductUrl.Should().Be("https://coop.ch/p/1");
        testee.Quantity.Should().Be(2);
        testee.Price.Should().Be("1.95");
        testee.Status.Should().Be(SessionItemStatus.Skipped);
    }

    [Test]
    public void DefaultConstructor_SetsStringPropertiesToEmptyString()
    {
        // Act — EF materializes instances via the protected parameterless constructor.
        var testee = (ShoppingSessionItem)Activator.CreateInstance(typeof(ShoppingSessionItem), nonPublic: true)!;

        // Assert
        testee.OriginalIngredient.Should().Be(string.Empty);
        testee.SelectedProductName.Should().Be(string.Empty);
        testee.SelectedProductUrl.Should().Be(string.Empty);
        testee.Price.Should().Be(string.Empty);
    }
}
