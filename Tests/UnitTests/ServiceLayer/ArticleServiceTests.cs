using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using NSubstitute;
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
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.CreateArticle(newArticleDto);

        context.Articles.Should().Contain(x => x.Name == "Cheese");
    }

    private static ArticleService CreateTestee(IArticleAction articleActionMock, InMemoryDbContext context)
    {
        var mapper = TestMapper.Create();
        var testee = new ArticleService(articleActionMock, context, mapper, new SimpleCrudHelper(context, mapper));
        return testee;
    }

    [Test]
    public void DeleteArticle()
    {
        using var context = new InMemoryDbContext();
        var deleteArticleGroupDto = new DeleteArticleDto(3);
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.DeleteArticle(deleteArticleGroupDto);

        articleActionMock.Received(1).DeleteArticle(deleteArticleGroupDto);
    }

    [Test]
    public void GetAllArticles()
    {
        using var context = new InMemoryDbContext();
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        testee.GetAllArticles();

        articleActionMock.Received(1).GetAllArticles();
    }
}