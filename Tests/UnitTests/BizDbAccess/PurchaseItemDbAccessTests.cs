using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class PurchaseItemDbAccessTests
{
    [Test]
    public void GetPurchaseItem()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var purchaseItem = inMemoryDbContext.PurchaseItems.Add(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        var result = testee.GetPurchaseItem(purchaseItem.Entity.PurchaseItemId);

        result.Article.Name.Should().Be("Tomato");
    }
        
    [Test]
    public void CreatePurchaseItem()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        var result = testee.AddPurchaseItem(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.PurchaseItems.Should().Contain(result);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var purchaseItem = inMemoryDbContext.PurchaseItems.Add(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        testee.DeletePurchaseItem(purchaseItem.Entity);
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.PurchaseItems.Should().NotContain(purchaseItem.Entity);
    }
}