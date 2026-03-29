using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class PurchaseItemDbAccessTests
{
    [Test]
    public void GetPurchaseItem()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var purchaseItem = inMemoryDbContext.PurchaseItems.Add(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        var result = testee.GetPurchaseItem(purchaseItem.Entity.PurchaseItemId);

        // Assert
        result.Article.Name.Should().Be("Tomato");
    }

    [Test]
    public void CreatePurchaseItem()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        var result = testee.AddPurchaseItem(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.PurchaseItems.Should().Contain(result);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var purchaseItem = inMemoryDbContext.PurchaseItems.Add(new PurchaseItem(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        testee.DeletePurchaseItem(purchaseItem.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.PurchaseItems.Should().NotContain(purchaseItem.Entity);
    }
}
