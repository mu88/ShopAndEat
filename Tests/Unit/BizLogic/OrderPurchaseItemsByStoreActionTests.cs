using BizLogic.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class OrderPurchaseItemsByStoreActionTests
{
    [Test]
    public void OrderPurchaseItemsByStore()
    {
        // Arrange
        var dairy = new ArticleGroup("Dairy");
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article("Tomato", vegetables, isInventory: false);
        var milk = new Article("Milk", dairy, isInventory: false);
        var unit = new UnitBuilder().WithDefaults().Build();
        var purchaseItem1 = new PurchaseItem(tomato, 1, unit);
        var purchaseItem2 = new PurchaseItem(milk, 3, unit);
        var shoppingOrder1 = new ShoppingOrder(vegetables, 50);
        var shoppingOrder2 = new ShoppingOrder(dairy, 30);
        var compartments = new[] { shoppingOrder1, shoppingOrder2 };
        // kept as direct construction: the specific compartments/order values are asserted on below
        var store = new Store("London", compartments);
        var purchaseItems = new[] { purchaseItem1, purchaseItem2 };
        var testee = new OrderPurchaseItemsByStoreAction();

        // Act
        var results = testee.OrderPurchaseItemsByStore(store, purchaseItems).ToList();

        // Assert — dairy's compartment Order (30) is lower than vegetables' (50), so the milk item must
        // come first; using .Equal (not .BeEquivalentTo) enforces the actual sequence, not just membership.
        results.Should().Equal(purchaseItem2, purchaseItem1);
    }

    [Test]
    public void OrderPurchaseItemsByStore_OrdersByArticleNameThenUnitName_WhenCompartmentOrderIsEqual()
    {
        // Arrange
        var vegetables = new ArticleGroupBuilder().WithDefaults().Build();
        var apple = new Article("Apple", vegetables, isInventory: false);
        var zucchini = new Article("Zucchini", vegetables, isInventory: false);
        var kg = new global::DataLayer.EfClasses.Unit("Kg");
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        var appleInPiece = new PurchaseItem(apple, 2, piece);
        var appleInKg = new PurchaseItem(apple, 1, kg);
        var zucchiniItem = new PurchaseItem(zucchini, 1, kg);
        // kept as direct construction: needs to share the exact "vegetables" reference used by the articles above
        var store = new Store("London", [new ShoppingOrder(vegetables, 10)]);
        var testee = new OrderPurchaseItemsByStoreAction();

        // Act — deliberately pass items out of the expected order.
        var results = testee.OrderPurchaseItemsByStore(store, [zucchiniItem, appleInPiece, appleInKg]).ToList();

        // Assert: same compartment Order -> ThenBy(Article.Name) ascending -> Apple before Zucchini;
        // same Article.Name -> ThenBy(Unit.Name) ascending -> "Kg" before "Piece".
        results.Should().Equal(appleInKg, appleInPiece, zucchiniItem);
    }

    [Test]
    public void OrderPurchaseItemsByStore_Throws_WhenNoCompartmentMatchesArticleGroup()
    {
        // Arrange
        var dairy = new ArticleGroup("Dairy");
        // kept as direct construction: needs a distinct ArticleGroup reference from the (builder-default "Vegetables")
        // article group used by the purchase item below, so that no store compartment matches
        var purchaseItem = new PurchaseItemBuilder().WithDefaults().Build();
        // Store only has a compartment for Dairy, not Vegetables.
        var store = new Store("London", [new ShoppingOrder(dairy, 10)]);
        var testee = new OrderPurchaseItemsByStoreAction();

        // Act — must use Single (throws), not SingleOrDefault (would silently return null and NRE later).
        var act = () => testee.OrderPurchaseItemsByStore(store, [purchaseItem]).ToList();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
