using BizDbAccess.Concrete;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class PurchaseItemDbAccessTests
{
    [Test]
    public async Task GetPurchaseItemAsync()
    {
        // Arrange
        await using var inMemoryDbContext = new InMemoryDbContext();
        var purchaseItem = new PurchaseItemBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(purchaseItem.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(purchaseItem.Article);
        inMemoryDbContext.Units.Add(purchaseItem.Unit);
        var purchaseItemEntry = inMemoryDbContext.PurchaseItems.Add(purchaseItem);
        await inMemoryDbContext.SaveChangesAsync();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        var result = await testee.GetPurchaseItemAsync(purchaseItemEntry.Entity.PurchaseItemId);

        // Assert
        result.Article.Name.Should().Be(purchaseItem.Article.Name);
    }

    [Test]
    public void CreatePurchaseItem()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var purchaseItem = new PurchaseItemBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(purchaseItem.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(purchaseItem.Article);
        inMemoryDbContext.Units.Add(purchaseItem.Unit);
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        var result = testee.AddPurchaseItem(purchaseItem);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.PurchaseItems.Should().Contain(result);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var purchaseItem = new PurchaseItemBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(purchaseItem.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(purchaseItem.Article);
        inMemoryDbContext.Units.Add(purchaseItem.Unit);
        var purchaseItemEntry = inMemoryDbContext.PurchaseItems.Add(purchaseItem);
        inMemoryDbContext.SaveChanges();
        var testee = new PurchaseItemDbAccess(inMemoryDbContext);

        // Act
        testee.DeletePurchaseItem(purchaseItemEntry.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.PurchaseItems.Should().NotContain(purchaseItemEntry.Entity);
    }
}
