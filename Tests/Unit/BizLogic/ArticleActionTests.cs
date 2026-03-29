using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class ArticleActionTests
{
    [Test]
    public void CreateArticle()
    {
        // Arrange
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(3, "Diary"), true);
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        articleDbAccessMock.AddArticle(Arg.Any<Article>()).Returns(call => call.Arg<Article>());
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        testee.CreateArticle(newArticleDto);

        // Assert
        articleDbAccessMock.Received(1).AddArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public void DeleteArticle()
    {
        // Arrange
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        articleDbAccessMock.GetArticle(3).Returns(new Article { Name = "Cheese", ArticleGroup = new ArticleGroup("Diary"), IsInventory = false });
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        testee.DeleteArticle(deleteArticleGroupDto);

        // Assert
        articleDbAccessMock.Received(1).DeleteArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public void GetAllArticles()
    {
        // Arrange
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        testee.GetAllArticles();

        // Assert
        articleDbAccessMock.Received(1).GetArticles();
    }
}
