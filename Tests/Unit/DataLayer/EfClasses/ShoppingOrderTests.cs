using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ShoppingOrderTests
{
    [Test]
    public void CreateShoppingOrder()
    {
        // Arrange
        var order = 3;
        var ingredientGroup = new ArticleGroup("Vegetables");

        // Act
        var testee = new ShoppingOrder(ingredientGroup, order);

        // Assert
        testee.ArticleGroup.Should().Be(ingredientGroup);
        testee.Order.Should().Be(order);
    }
}
