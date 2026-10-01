using System.Diagnostics;
using DataLayer.EfClasses;
using DTO.Store;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class StoreServiceTests
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

    [Test]
    public async Task GetAllStoresAsync_WithNoStores_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = CreateTestee(context);

        // Act
        var results = await testee.GetAllStoresAsync();

        // Assert
        results.Should().BeEmpty();
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "StoreService.GetAllStoresAsync");
    }

    [Test]
    public async Task GetAllStoresAsync_WithSingleStore_ReturnsStore()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var store = context.Stores.Add(new StoreBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = (await testee.GetAllStoresAsync()).ToList();

        // Assert
        results.Should().HaveCount(1).And.SatisfyRespectively(first => first.Name.Should().Be(store.Entity.Name));
    }

    [Test]
    public async Task GetAllStoresAsync_WithMultipleStores_ReturnsAllStores()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.Stores.AddRange(
            new Store("Store A", Enumerable.Empty<ShoppingOrder>()),
            new Store("Store B", Enumerable.Empty<ShoppingOrder>()),
            new Store("Store C", Enumerable.Empty<ShoppingOrder>()));
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = (await testee.GetAllStoresAsync()).ToList();

        // Assert
        results.Should().HaveCount(3).And.AllSatisfy(
            store => store.StoreId.Value.Should().BeGreaterThan(0)).And.Satisfy(
            first => first.Name == "Store A",
            second => second.Name == "Store B",
            third => third.Name == "Store C");
    }

    [Test]
    public async Task GetAllStoresAsync_WithStoresWithCompartments_ReturnsStoresWithCorrectInfo()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var stores = new[]
        {
            new StoreBuilder().WithDefaults().Build(),
            new StoreBuilder().WithDefaults().Build()
        };
        context.Stores.AddRange(stores);
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = (await testee.GetAllStoresAsync()).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.TrueForAll(store => store.Name != null).Should().BeTrue();
        results.TrueForAll(store => store.StoreId.Value > 0).Should().BeTrue();
    }

    [Test]
    public async Task GetAllStoresAsync_MapsToDtoCorrectly()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var store = context.Stores.Add(new StoreBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var storeId = store.Entity.StoreId;
        var testee = CreateTestee(context);

        // Act
        var results = (await testee.GetAllStoresAsync()).ToList();

        // Assert
        results.Should().HaveCount(1);
        var resultStore = results[0];
        resultStore.Should().BeOfType<ExistingStoreDto>();
        resultStore.StoreId.Should().Be(storeId);
        resultStore.Name.Should().Be(store.Entity.Name);
    }

    private static StoreService CreateTestee(InMemoryDbContext context)
        => new(new SimpleCrudHelper(context));
}
