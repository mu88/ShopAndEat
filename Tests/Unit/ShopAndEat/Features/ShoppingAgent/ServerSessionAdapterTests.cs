using BizDbAccess;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Store;
using DTO.Unit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer;
using ShopAndEat.Features.ShoppingAgent.Adapters;
using ShoppingAgent.Services;
using Tests.Builders;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Unit.ShopAndEat.Features.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ServerSessionAdapterTests
{
    private static readonly string[] ExpectedUnits = ["Bunch", "Gram", "Piece"];
    [Test]
    public async Task CreateSessionAsync_CreatesSession_ReturnsId()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        sessionRepo.CreateSessionAsync(Arg.Any<ShoppingSession>(), default)
            .Returns(new ShoppingSessionId(42));
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        var result = await testee.CreateSessionAsync("Milk, Eggs");

        // Assert
        result.Should().Be(42);
        await sessionRepo.Received(1).CreateSessionAsync(
            Arg.Is<ShoppingSession>(s => s.IngredientList == "Milk, Eggs"),
            default);
    }

    [Test]
    public async Task AddSessionItemAsync_AddsItem_WhenSessionExists()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        var session = new ShoppingSessionBuilder().WithDefaults().Build();
        sessionRepo.FindSessionAsync(new ShoppingSessionId(10), default).Returns(session);
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        await testee.AddSessionItemAsync(10, new SessionItemDto
        {
            OriginalIngredient = "1L Milk",
            SelectedProductName = "Bio Milch",
            SelectedProductUrl = "https://example.test/milch",
            Quantity = 2,
            Price = "2.50",
        });

        // Assert — proves all DTO fields are forwarded, not just OriginalIngredient (which, due to the
        // private setters introduced for immutability, can never be corrected after construction).
        await sessionRepo.Received(1).AddItemToSessionAsync(
            Arg.Is<ShoppingSessionItem>(i =>
                i.OriginalIngredient == "1L Milk" &&
                i.SelectedProductName == "Bio Milch" &&
                i.SelectedProductUrl == "https://example.test/milch" &&
                i.Quantity == 2 &&
                i.Price == "2.50"),
            default);
    }

    [Test]
    public async Task AddSessionItemAsync_LogsWarning_WhenSessionNotFound()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        sessionRepo.FindSessionAsync(Arg.Any<ShoppingSessionId>(), default).Returns((ShoppingSession?)null);
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        await testee.AddSessionItemAsync(99, new SessionItemDto { OriginalIngredient = "Toast" });

        // Assert
        await sessionRepo.DidNotReceive().AddItemToSessionAsync(Arg.Any<ShoppingSessionItem>(), default);
    }

    [Test]
    public async Task CompleteSessionAsync_CompletesSession()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        var session = new ShoppingSessionBuilder().WithDefaults().Build();
        sessionRepo.FindSessionAsync(new ShoppingSessionId(5), default).Returns(session);
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        await testee.CompleteSessionAsync(5);

        // Assert
        await sessionRepo.Received(1).CompleteSessionAsync(session, default);
    }

    [Test]
    public async Task CompleteSessionAsync_DoesNotThrow_WhenSessionNotFound()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        sessionRepo.FindSessionAsync(Arg.Any<ShoppingSessionId>(), default).Returns((ShoppingSession?)null);
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        var act = async () => await testee.CompleteSessionAsync(99);

        // Assert
        await act.Should().NotThrowAsync();
        await sessionRepo.DidNotReceive().CompleteSessionAsync(Arg.Any<ShoppingSession>(), default);
    }

    [Test]
    public async Task GetIngredientListAsync_ReturnsEmpty_WhenNoStore()
    {
        // Arrange
        await using var db = new InMemoryDbContext();
        var testee = CreateTestee(db: db);

        // Act
        var result = await testee.GetIngredientListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetIngredientListAsync_MapsItemsFromMealService()
    {
        // Arrange
        await using var db = new InMemoryDbContext();
        db.Stores.Add(new StoreBuilder().WithDefaults().Build());
        await db.SaveChangesAsync();

        var mealService = Substitute.For<IMealService>();
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Dairy");
        var article = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Milk", articleGroup, false);
        var unit = new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "liter");
        mealService.GetOrderedPurchaseItemsAsync(Arg.Any<ExistingStoreDto>())
            .Returns(Task.FromResult<IReadOnlyList<NewPurchaseItemDto>>([new NewPurchaseItemDto(article, unit, 2.0)]));
        var testee = CreateTestee(db: db, mealService: mealService);

        // Act
        var result = await testee.GetIngredientListAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Article.Should().Be("Milk");
        result[0].Quantity.Should().Be(2.0);
        result[0].Unit.Should().Be("liter");
    }

    [Test]
    public async Task GetIngredientListAsync_WithMultipleStores_UsesFirstStoreOrderedByIdAscending()
    {
        // Arrange — a Sqlite (not InMemory-provider) database is used because ordering by StoreId
        // (a readonly record struct without IComparable) requires real SQL translation of ORDER BY;
        // the InMemory provider would need to compare StoreId instances client-side and throw.
        await using var db = new SqliteDbContext();
        var secondStore = db.Stores.Add(new StoreBuilder().WithDefaults().Build()).Entity;
        var firstStore = db.Stores.Add(new StoreBuilder().WithDefaults().Build()).Entity;
        await db.SaveChangesAsync();

        var mealService = Substitute.For<IMealService>();
        mealService.GetOrderedPurchaseItemsAsync(Arg.Any<ExistingStoreDto>())
            .Returns(Task.FromResult<IReadOnlyList<NewPurchaseItemDto>>([]));
        var testee = CreateTestee(db: db, mealService: mealService);

        // Act
        await testee.GetIngredientListAsync();

        // Assert
        var expectedFirstStoreId = new[] { firstStore.StoreId, secondStore.StoreId }.Min(id => id.Value);
        await mealService.Received(1).GetOrderedPurchaseItemsAsync(
            Arg.Is<ExistingStoreDto>(dto => dto.StoreId.Value == expectedFirstStoreId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetUnitsAsync_ReturnsOrderedUnits()
    {
        // Arrange
        await using var db = new InMemoryDbContext();
        db.Units.AddRange(new EfUnit("Piece"), new EfUnit("Gram"), new EfUnit("Bunch"));
        await db.SaveChangesAsync();
        var testee = CreateTestee(db: db);

        // Act
        var result = (await testee.GetUnitsAsync()).ToList();

        // Assert
        result.Should().BeEquivalentTo(ExpectedUnits, options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetSessionsAsync_MapsSessionsToSummaries()
    {
        // Arrange
        var sessionRepo = Substitute.For<ISessionRepository>();
        var startedAt = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var session = new ShoppingSession("Milk, Eggs", startedAt);
        session.Complete(startedAt.AddHours(1));
        sessionRepo.GetAllSessionsAsync(20, default).Returns([session]);
        var testee = CreateTestee(sessionRepo: sessionRepo);

        // Act
        var result = await testee.GetSessionsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].StartedAt.Should().Be(startedAt);
        result[0].Status.Should().Be("Completed");
        result[0].IngredientList.Should().Be("Milk, Eggs");
    }

    private static ServerSessionAdapter CreateTestee(
        ISessionRepository? sessionRepo = null,
        IMealService? mealService = null,
        EfCoreContext? db = null)
    {
        return new ServerSessionAdapter(
            sessionRepo ?? Substitute.For<ISessionRepository>(),
            mealService ?? Substitute.For<IMealService>(),
            db ?? new InMemoryDbContext(),
            TimeProvider.System,
            NullLogger<ServerSessionAdapter>.Instance);
    }
}
