using System.Diagnostics;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class PurchaseItemServiceTests
{
    private readonly List<Activity> _completedActivities = [];
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ServiceLayerDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

    private static DbContextOptions<EfCoreContext> CreateSharedDbOptions(string dbName)
        => new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(dbName).Options;

    [Test]
    public async Task CreatePurchaseItemAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var newPurchaseItemDto =
            new NewPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables"), false),
                new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "Piece"),
                2);
        var purchaseItemActionMock = Substitute.For<IPurchaseItemAction>();
        var testee = new PurchaseItemService(purchaseItemActionMock, context);

        // Act
        await testee.CreatePurchaseItemAsync(newPurchaseItemDto);

        // Assert
        purchaseItemActionMock.Received(1).CreatePurchaseItem(newPurchaseItemDto);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "PurchaseItemService.CreatePurchaseItemAsync");
    }

    [Test]
    public async Task CreatePurchaseItemAsync_WithRealPurchaseItemAction_PersistsCreation()
    {
        // Arrange — uses the real PurchaseItemAction/PurchaseItemDbAccess (not a mock), which only
        // calls context.PurchaseItems.Add() without saving, to prove PurchaseItemService's own
        // SaveChangesAsync is what actually persists the creation. Reads back via a SECOND context
        // instance on the same shared in-memory DB name, since the first context's change tracker
        // would otherwise mask a missing SaveChangesAsync call.
        var dbName = Guid.NewGuid().ToString();
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var articleGroup = writeContext.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build()).Entity;
            var article = writeContext.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
            var unit = writeContext.Units.Add(new UnitBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();

            var newPurchaseItemDto = new NewPurchaseItemDto(
                new ExistingArticleDto(article.ArticleId, article.Name, new ExistingArticleGroupDto(articleGroup.ArticleGroupId, articleGroup.Name), false),
                new ExistingUnitDto(unit.UnitId, unit.Name),
                2);
            var purchaseItemAction = new PurchaseItemAction(new PurchaseItemDbAccess(writeContext));
            var testee = new PurchaseItemService(purchaseItemAction, writeContext);

            // Act
            await testee.CreatePurchaseItemAsync(newPurchaseItemDto);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.PurchaseItems.Should().ContainSingle();
    }

    [Test]
    public async Task DeletePurchaseItemAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemActionMock = Substitute.For<IPurchaseItemAction>();
        purchaseItemActionMock.DeletePurchaseItemAsync(deletePurchaseItemGroupDto).Returns(Task.CompletedTask);
        var testee = new PurchaseItemService(purchaseItemActionMock, context);

        // Act
        await testee.DeletePurchaseItemAsync(deletePurchaseItemGroupDto);

        // Assert
        await purchaseItemActionMock.Received(1).DeletePurchaseItemAsync(deletePurchaseItemGroupDto);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "PurchaseItemService.DeletePurchaseItemAsync");
    }

    [Test]
    public async Task DeletePurchaseItemAsync_WithRealPurchaseItemAction_PersistsDeletion()
    {
        // Arrange — uses the real PurchaseItemAction/PurchaseItemDbAccess (not a mock), which only
        // calls context.PurchaseItems.Remove() without saving, to prove PurchaseItemService's own
        // SaveChangesAsync is what actually persists the deletion.
        var dbName = Guid.NewGuid().ToString();
        int purchaseItemId;
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var purchaseItem = writeContext.PurchaseItems.Add(new PurchaseItemBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();
            purchaseItemId = purchaseItem.PurchaseItemId;

            var purchaseItemAction = new PurchaseItemAction(new PurchaseItemDbAccess(writeContext));
            var testee = new PurchaseItemService(purchaseItemAction, writeContext);
            var deletePurchaseItemDto = new DeletePurchaseItemDto(purchaseItemId);

            // Act
            await testee.DeletePurchaseItemAsync(deletePurchaseItemDto);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.PurchaseItems.Should().BeEmpty();
    }
}
