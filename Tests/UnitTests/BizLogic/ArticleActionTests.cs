using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using Moq;
using NUnit.Framework;
using Tests.Doubles;

namespace Tests.UnitTests.BizLogic;

[TestFixture]
[Category("Unit")]
public class ArticleActionTests
{
    [Test]
    public void CreateArticle()
    {
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(3, "Diary"), true);
        var articleDbAccessMock = new Mock<IArticleDbAccess>();
        var testee = new ArticleAction(articleDbAccessMock.Object, TestMapper.Create());

        testee.CreateArticle(newArticleDto);

        articleDbAccessMock.Verify(x => x.AddArticle(It.Is<Article>(a => a.Name == "Cheese")), Times.Once);
    }

    [Test]
    public void DeleteArticle()
    {
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleDbAccessMock = new Mock<IArticleDbAccess>();
        articleDbAccessMock.Setup(x => x.GetArticle(3)).Returns(new Article{Name = "Cheese", ArticleGroup = new ArticleGroup("Diary"),IsInventory = false});
        var testee = new ArticleAction(articleDbAccessMock.Object, TestMapper.Create());

        testee.DeleteArticle(deleteArticleGroupDto);

        articleDbAccessMock.Verify(x => x.DeleteArticle(It.Is<Article>(a => a.Name == "Cheese")), Times.Once);
    }

    [Test]
    public void GetAllArticles()
    {
        var articleDbAccessMock = new Mock<IArticleDbAccess>();
        var testee = new ArticleAction(articleDbAccessMock.Object, TestMapper.Create());

        testee.GetAllArticles();

        articleDbAccessMock.Verify(x => x.GetArticles(), Times.Once);
    }
}