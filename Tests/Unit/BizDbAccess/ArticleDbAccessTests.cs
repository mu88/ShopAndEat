using BizDbAccess.Concrete;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class ArticleDbAccessTests
{
    [Test]
    public async Task GetArticleAsync()
    {
        // Arrange
        await using var inMemoryDbContext = new InMemoryDbContext();
        var tomato = new ArticleBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(tomato.ArticleGroup);
        var tomatoEntry = inMemoryDbContext.Articles.Add(tomato);
        await inMemoryDbContext.SaveChangesAsync();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        var result = await testee.GetArticleAsync(tomatoEntry.Entity.ArticleId);

        // Assert
        result.Name.Should().Be(tomato.Name);
    }

    [Test]
    public async Task GetArticlesAsync()
    {
        // Arrange
        await using var inMemoryDbContext = new InMemoryDbContext();
        var article = new ArticleBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(article.ArticleGroup);
        inMemoryDbContext.Articles.Add(article);
        await inMemoryDbContext.SaveChangesAsync();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        var result = await testee.GetArticlesAsync();

        // Assert
        result.Should().Contain(x => x.Name == article.Name);
    }

    [Test]
    public void CreateArticle()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var article = new ArticleBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(article.ArticleGroup);
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        testee.AddArticle(article);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Articles.Should().Contain(x => x.Name == article.Name);
    }

    [Test]
    public void DeleteArticle()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var tomato = new ArticleBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(tomato.ArticleGroup);
        var tomatoEntry = inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.SaveChanges();
        var testee = new ArticleDbAccess(inMemoryDbContext);

        // Act
        testee.DeleteArticle(tomatoEntry.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Articles.Should().NotContain(x => x.Name == tomato.Name);
    }
}
