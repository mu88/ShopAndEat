using BizDbAccess;
using DataLayer.EfClasses;
using DTO.OnlineArticleMapping;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Api;
using Tests.Builders;

namespace Tests.Unit.ShopAndEat.Api;

[TestFixture]
[Category("Unit")]
public class OnlineShoppingControllerTests
{
    [Test]
    public async Task SaveArticleMapping_MapsDtoWithRouteStoreKey_AndSaves()
    {
        // Arrange
        var articleMappingRepositoryMock = Substitute.For<IArticleMappingRepository>();
        var testee = new OnlineShoppingController(articleMappingRepositoryMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var mappingDto = new NewOnlineArticleMappingDto { ArticleName = "Milch", StoreProductCode = "123" };

        // Act
        await testee.SaveArticleMapping("coop", mappingDto, testee.HttpContext.RequestAborted);

        // Assert
        await articleMappingRepositoryMock.Received(1).SaveOrUpdateMappingAsync(
            Arg.Is<OnlineArticleMapping>(mapping => mapping.ArticleName == "Milch" && mapping.StoreKey == "coop"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetArticleMapping_WhenMappingExists_ReturnsOk()
    {
        // Arrange
        var articleMappingRepositoryMock = Substitute.For<IArticleMappingRepository>();
        var mapping = new OnlineArticleMapping("Milch", "coop", "123", DateTimeOffset.UtcNow);
        articleMappingRepositoryMock.GetMappingAsync("coop", "Milch").Returns(mapping);
        var testee = new OnlineShoppingController(articleMappingRepositoryMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetArticleMapping("coop", "Milch", testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ExistingOnlineArticleMappingDto>>()
            .Which.Value!.ArticleName.Should().Be("Milch");
    }

    [Test]
    public async Task GetArticleMapping_WhenMappingDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        var articleMappingRepositoryMock = Substitute.For<IArticleMappingRepository>();
        articleMappingRepositoryMock.GetMappingAsync("coop", "Milch").Returns((OnlineArticleMapping?)null);
        var testee = new OnlineShoppingController(articleMappingRepositoryMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetArticleMapping("coop", "Milch", testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Contain("Milch").And.Contain("coop");
    }

    [Test]
    public async Task GetAllArticleMappings_ReturnsAllMappedDtos()
    {
        // Arrange
        var articleMappingRepositoryMock = Substitute.For<IArticleMappingRepository>();
        var mappingOne = new OnlineArticleMappingBuilder().WithDefaults().Build();
        var mappingTwo = new OnlineArticleMappingBuilder().WithDefaults().Build();
        articleMappingRepositoryMock.GetAllMappingsAsync("coop", "Milch").Returns([mappingOne, mappingTwo]);
        var testee = new OnlineShoppingController(articleMappingRepositoryMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetAllArticleMappings("coop", "Milch", testee.HttpContext.RequestAborted);

        // Assert
        result.Value.Should().HaveCount(2);
    }
}
