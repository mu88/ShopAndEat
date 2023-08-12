using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Doubles;

namespace Tests.UnitTests.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class ArticleServiceTests
{
    [Test]
    public void CreateArticle()
    {
        using var context = new InMemoryDbContext();
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(3, "Diary"), true);
        var articleActionMock = new Mock<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.CreateArticle(newArticleDto);

        context.Articles.Should().Contain(x => x.Name == "Cheese");
    }

    private static ArticleService CreateTestee(Mock<IArticleAction> articleActionMock, InMemoryDbContext context)
    {
        var mapper = TestMapper.Create();
        var testee = new ArticleService(articleActionMock.Object, context, mapper, new SimpleCrudHelper(context, mapper));
        return testee;
    }

    [Test]
    public void DeleteArticle()
    {
        using var context = new InMemoryDbContext();
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleActionMock = new Mock<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.DeleteArticle(deleteArticleGroupDto);

        articleActionMock.Verify(x => x.DeleteArticle(deleteArticleGroupDto), Times.Once);
    }

    [Test]
    public void GetAllArticles()
    {
        using var context = new InMemoryDbContext();
        var articleActionMock = new Mock<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.GetAllArticles();

        articleActionMock.Verify(x => x.GetAllArticles(), Times.Once);
    }
}