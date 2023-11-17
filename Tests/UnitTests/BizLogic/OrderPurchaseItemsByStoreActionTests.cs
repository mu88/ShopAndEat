using BizLogic.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.BizLogic;

[TestFixture]
[Category("Unit")]
public class OrderPurchaseItemsByStoreActionTests
{
    [Test]
    public void OrderPurchaseItemsByStore()
    {
        var dairy = new ArticleGroup("Dairy");
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var milk = new Article{Name="Milk", ArticleGroup = dairy, IsInventory = false};
        var bag = new Unit("Bag");
        var purchaseItem1 = new PurchaseItem(tomato, 1, bag);
        var purchaseItem2 = new PurchaseItem(milk, 3, bag);
        var shoppingOrder1 = new ShoppingOrder(vegetables, 50);
        var shoppingOrder2 = new ShoppingOrder(dairy, 30);
        var compartments = new[] { shoppingOrder1, shoppingOrder2 };
        var store = new Store("London", compartments);
        var purchaseItems = new[] { purchaseItem1, purchaseItem2 };
        var testee = new OrderPurchaseItemsByStoreAction();

        var results = testee.OrderPurchaseItemsByStore(store, purchaseItems).ToList();

        results.Should().HaveCount(2);
        results.Should().BeEquivalentTo(new []{purchaseItem2, purchaseItem1});
    }
}