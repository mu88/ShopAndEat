using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class PurchaseItemTests
{
    [Test]
    public void CreatePurchaseItem()
    {
        var article = new Article{Name="Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false};
        uint quantity = 3;
        var unit = new Unit("Bag");

        var testee = new PurchaseItem(article, quantity, unit);

        testee.Article.Should().Be(article);
        testee.Quantity.Should().Be(quantity);
        testee.Unit.Should().Be(unit);
    }
}