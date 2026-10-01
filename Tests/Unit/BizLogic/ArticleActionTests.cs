using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class ArticleActionTests
{
    [Test]
    public void CreateArticle()
    {
        // Arrange
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(3), "Diary"), true);
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        articleDbAccessMock.AddArticle(Arg.Any<Article>()).Returns(call => call.Arg<Article>());
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        testee.CreateArticle(newArticleDto);

        // Assert
        articleDbAccessMock.Received(1).AddArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public async Task DeleteArticleAsync()
    {
        // Arrange
        var deleteArticleGroupDto = new DeleteArticleDto(new global::DataLayer.EfClasses.ArticleId(3));
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        articleDbAccessMock.GetArticleAsync(new global::DataLayer.EfClasses.ArticleId(3)).Returns(Task.FromResult(new Article("Cheese", new ArticleGroupBuilder().WithDefaults().Build(), isInventory: false)));
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        await testee.DeleteArticleAsync(deleteArticleGroupDto);

        // Assert
        articleDbAccessMock.Received(1).DeleteArticle(Arg.Is<Article>(a => a.Name == "Cheese"));
    }

    [Test]
    public async Task GetAllArticlesAsync()
    {
        // Arrange
        var articleDbAccessMock = Substitute.For<IArticleDbAccess>();
        var article = new Article("Cheese", new ArticleGroupBuilder().WithDefaults().Build(), isInventory: false);
        articleDbAccessMock.GetArticlesAsync().Returns(Task.FromResult<IEnumerable<Article>>(new[] { article }));
        var testee = new ArticleAction(articleDbAccessMock);

        // Act
        var result = await testee.GetAllArticlesAsync();

        // Assert
        await articleDbAccessMock.Received(1).GetArticlesAsync();
        result.Should().ContainSingle(dto => dto.Name == "Cheese");
    }
}
