using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class ArticleDbAccessTests
{
    [Test]
    public void GetArticle()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        var tomato = inMemoryDbContext.Articles.Add(new Article{Name="Tomato", ArticleGroup = vegetables.Entity, IsInventory = false});
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        var result = testee.GetArticle(tomato.Entity.ArticleId);

        result.Name.Should().Be("Tomato");
    }

    [Test]
    public void GetArticles()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        inMemoryDbContext.Articles.Add(new Article{Name="Tomato", ArticleGroup = vegetables.Entity, IsInventory = false});
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        var result = testee.GetArticles();

        result.Should().Contain(x => x.Name == "Tomato");
    }

    [Test]
    public void CreateArticle()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        testee.AddArticle(new Article{Name="Tomato", ArticleGroup = vegetables.Entity, IsInventory = false});
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.Articles.Should().Contain(x => x.Name == "Tomato");
    }

    [Test]
    public void DeleteArticle()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = inMemoryDbContext.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        var tomato = inMemoryDbContext.Articles.Add(new Article{Name="Tomato", ArticleGroup = vegetables.Entity, IsInventory = false});
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        testee.DeleteArticle(tomato.Entity);
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.Articles.Should().NotContain(x => x.Name == "Tomato");
    }
}