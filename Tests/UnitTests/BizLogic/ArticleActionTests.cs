using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using NSubstitute;
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
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        var testee = new ArticleAction(articleDbAccessMock, TestMapper.Create());

        testee.CreateArticle(newArticleDto);

        articleDbAccessMock.Received(1).AddArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public void DeleteArticle()
    {
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        articleDbAccessMock.GetArticle(3).Returns(new Article{Name = "Cheese", ArticleGroup = new ArticleGroup("Diary"),IsInventory = false});
        var testee = new ArticleAction(articleDbAccessMock, TestMapper.Create());

        testee.DeleteArticle(deleteArticleGroupDto);

        articleDbAccessMock.Received(1).DeleteArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public void GetAllArticles()
    {
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        var testee = new ArticleAction(articleDbAccessMock, TestMapper.Create());

        testee.GetAllArticles();

        articleDbAccessMock.Received(1).GetArticles();
    }
}