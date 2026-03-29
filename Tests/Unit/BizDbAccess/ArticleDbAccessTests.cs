using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class ArticleDbAccessTests
{
    [Test]
    public void GetArticle()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        var tomato = inMemoryDbContext.Articles.Add(new Article { Name = "Tomato", ArticleGroup = vegetables.Entity, IsInventory = false });
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        var result = testee.GetArticle(tomato.Entity.ArticleId);

        // Assert
        result.Name.Should().Be("Tomato");
    }

    [Test]
    public void GetArticles()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        inMemoryDbContext.Articles.Add(new Article { Name = "Tomato", ArticleGroup = vegetables.Entity, IsInventory = false });
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        var result = testee.GetArticles();

        // Assert
        result.Should().Contain(x => x.Name == "Tomato");
    }

    [Test]
    public void CreateArticle()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        testee.AddArticle(new Article { Name = "Tomato", ArticleGroup = vegetables.Entity, IsInventory = false });
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Articles.Should().Contain(x => x.Name == "Tomato");
    }

    [Test]
    public void DeleteArticle()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        var tomato = inMemoryDbContext.Articles.Add(new Article { Name = "Tomato", ArticleGroup = vegetables.Entity, IsInventory = false });
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        testee.DeleteArticle(tomato.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Articles.Should().NotContain(x => x.Name == "Tomato");
    }
}
