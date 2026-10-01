using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer;
using ShopAndEat.Api;
using ShopAndEat.Api.Resources;

namespace Tests.Unit.ShopAndEat.Api;

[TestFixture]
[Category("Unit")]
public class IngredientListControllerTests
{
    private IMealService _mealServiceMock = null!;
    private IStringLocalizer<Messages> _localizerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _mealServiceMock = Substitute.For<IMealService>();
        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), $"{call.ArgAt<string>(0)}:{string.Join(',', call.ArgAt<object[]>(1))}"));
    }

    [Test]
    public async Task GetIngredientList_WithExplicitStoreId_WhenStoreExists_ReturnsFormattedItems()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var store = context.Stores.Add(new global::DataLayer.EfClasses.Store("Coop", [])).Entity;
        await context.SaveChangesAsync();
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var purchaseItem = new NewPurchaseItemDto(
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomaten", articleGroup, false),
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "Stück"),
            2);
        _mealServiceMock.GetOrderedPurchaseItemsAsync(Arg.Is<global::DTO.Store.ExistingStoreDto>(dto => dto.StoreId == store.StoreId))
            .Returns([purchaseItem]);
        var testee = new IngredientListController(_mealServiceMock, context, _localizerMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetIngredientList(store.StoreId.Value, testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<global::DTO.IngredientList.IngredientListResponse>>()
            .Which.Value!.Items.Should().ContainSingle(item => item.Article == "Tomaten" && item.Quantity == 2 && item.Unit == "Stück" && item.Text == "2 Stück Tomaten");
    }

    [Test]
    public async Task GetIngredientList_WithoutStoreId_UsesFirstStoreOrderedByIdAscending()
    {
        // Arrange — a Sqlite (not InMemory-provider) database is used because ordering by StoreId
        // (a readonly record struct without IComparable) requires real SQL translation of ORDER BY;
        // the InMemory provider would need to compare StoreId instances client-side and throw.
        await using var context = new SqliteDbContext();
        var secondStore = context.Stores.Add(new global::DataLayer.EfClasses.Store("Migros", [])).Entity;
        var firstStore = context.Stores.Add(new global::DataLayer.EfClasses.Store("Coop", [])).Entity;
        await context.SaveChangesAsync();
        _mealServiceMock.GetOrderedPurchaseItemsAsync(Arg.Any<global::DTO.Store.ExistingStoreDto>()).Returns([]);
        var testee = new IngredientListController(_mealServiceMock, context, _localizerMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        await testee.GetIngredientList(cancellationToken: testee.HttpContext.RequestAborted);

        // Assert
        var expectedFirstStoreId = new[] { firstStore.StoreId, secondStore.StoreId }.Min(id => id.Value);
        await _mealServiceMock.Received(1).GetOrderedPurchaseItemsAsync(
            Arg.Is<global::DTO.Store.ExistingStoreDto>(dto => dto.StoreId.Value == expectedFirstStoreId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetIngredientList_WithExplicitStoreId_WhenMultipleStoresExist_UsesFindAsyncNotFirstOrderedStore()
    {
        // Arrange — with an explicit storeId, the controller must look the store up directly instead of
        // falling back to the "first store ordered by ID" branch used when no storeId is supplied.
        await using var context = new InMemoryDbContext();
        var firstStore = context.Stores.Add(new global::DataLayer.EfClasses.Store("Coop", [])).Entity;
        var secondStore = context.Stores.Add(new global::DataLayer.EfClasses.Store("Migros", [])).Entity;
        await context.SaveChangesAsync();
        _mealServiceMock.GetOrderedPurchaseItemsAsync(Arg.Any<global::DTO.Store.ExistingStoreDto>()).Returns([]);
        var testee = new IngredientListController(_mealServiceMock, context, _localizerMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        await testee.GetIngredientList(secondStore.StoreId.Value, testee.HttpContext.RequestAborted);

        // Assert
        await _mealServiceMock.Received(1).GetOrderedPurchaseItemsAsync(
            Arg.Is<global::DTO.Store.ExistingStoreDto>(dto => dto.StoreId == secondStore.StoreId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetIngredientList_WithExplicitStoreId_WhenStoreDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new IngredientListController(_mealServiceMock, context, _localizerMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetIngredientList(999, testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("StoreNotFound:999");
    }

    [Test]
    public async Task GetIngredientList_WithoutStoreId_WhenNoStoresExist_ReturnsEmptyOkResult()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new IngredientListController(_mealServiceMock, context, _localizerMock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetIngredientList(cancellationToken: testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<global::DTO.IngredientList.IngredientListResponse>>()
            .Which.Value!.Items.Should().BeEmpty();
    }
}
