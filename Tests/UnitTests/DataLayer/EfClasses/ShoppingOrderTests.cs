using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ShoppingOrderTests
{
    [Test]
    public void CreateShoppingOrder()
    {
        var order = 3;
        var ingredientGroup = new ArticleGroup("Vegetables");

        var testee = new ShoppingOrder(ingredientGroup, order);

        testee.ArticleGroup.Should().Be(ingredientGroup);
        testee.Order.Should().Be(order);
    }
}