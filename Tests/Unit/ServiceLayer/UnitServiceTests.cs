using System.Diagnostics;
using DTO.Unit;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class UnitServiceTests
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
    public async Task CreateUnitAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new UnitService(new SimpleCrudHelper(context));
        var newUnitDto = new NewUnitDto("Piece");

        // Act
        await testee.CreateUnitAsync(newUnitDto);

        // Assert
        context.Units.Should().Contain(unit => unit.Name == "Piece");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "UnitService.CreateUnitAsync");
    }

    [Test]
    public async Task DeleteUnitAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existingUnit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = new UnitService(new SimpleCrudHelper(context));
        var deleteUnitDto = new DeleteUnitDto(existingUnit.Entity.UnitId);

        // Act
        await testee.DeleteUnitAsync(deleteUnitDto);

        // Assert
        context.Units.Should().NotContain(unit => unit.Name == existingUnit.Entity.Name);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "UnitService.DeleteUnitAsync");
    }

    [Test]
    public async Task GetAllUnitsAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Piece"));
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Bag"));
        await context.SaveChangesAsync();
        var testee = new UnitService(new SimpleCrudHelper(context));

        // Act
        var results = await testee.GetAllUnitsAsync();

        // Assert
        results.Should().Contain(unit => unit.Name == "Piece").And.Contain(unit => unit.Name == "Bag");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "UnitService.GetAllUnitsAsync");
    }
}
