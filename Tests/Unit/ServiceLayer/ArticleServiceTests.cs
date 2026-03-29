using BizLogic;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class ArticleServiceTests
{
    [Test]
    public void CreateArticle()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Diary"));
        context.SaveChanges();
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(articleGroup.Entity.ArticleGroupId, "Diary"), true);
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        // Act
        testee.CreateArticle(newArticleDto);

        // Assert
        context.Articles.Should().Contain(article => article.Name == "Cheese");
    }

    [Test]
    public void DeleteArticle()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        // Act
        testee.DeleteArticle(deleteArticleGroupDto);

        // Assert
        articleActionMock.Received(1).DeleteArticle(deleteArticleGroupDto);
    }

    [Test]
    public void GetAllArticles()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        // Act
        testee.GetAllArticles();

        // Assert
        articleActionMock.Received(1).GetAllArticles();
    }

    private static ArticleService CreateTestee(IArticleAction articleActionMock, InMemoryDbContext context)
        => new(articleActionMock, context, new SimpleCrudHelper(context));
}
