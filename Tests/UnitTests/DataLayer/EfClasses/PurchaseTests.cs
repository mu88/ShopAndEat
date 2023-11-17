using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class PurchaseTests
{
    [Test]
    public void CreatePurchase()
    {
        var from = new DateTime();
        var to = new DateTime();
        var purchaseItems = new Collection<PurchaseItem>
        {
            new PurchaseItem(new Article{Name="Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false}, 3, new Unit("Bag"))
        };

        var testee = new Purchase(from, to, purchaseItems);

        testee.From.Should().Be(from);
        testee.To.Should().Be(to);
        testee.PurchaseItems.Should().BeEquivalentTo(purchaseItems);
    }
}