using System.Diagnostics;
using DataLayer.EfClasses;
using DTO.MealType;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class MealTypeServiceTests
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
    public async Task CreateMealTypeAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var newMealTypeDto = new NewMealTypeDto("Lunch");

        // Act
        await testee.CreateMealTypeAsync(newMealTypeDto);

        // Assert
        context.MealTypes.Should().Contain(mealType => mealType.Name == "Lunch");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealTypeService.CreateMealTypeAsync");
    }

    [Test]
    public async Task DeleteMealTypeAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existingMealType = context.MealTypes.Add(new MealTypeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var deleteMealTypeDto = new DeleteMealTypeDto(existingMealType.Entity.MealTypeId);

        // Act
        await testee.DeleteMealTypeAsync(deleteMealTypeDto);

        // Assert
        context.MealTypes.Should().NotContain(mealType => mealType.Name == existingMealType.Entity.Name);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealTypeService.DeleteMealTypeAsync");
    }

    [Test]
    public async Task GetAllMealTypesAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.MealTypes.Add(new MealType("Lunch", 1));
        context.MealTypes.Add(new MealType("Breakfast", 2));
        await context.SaveChangesAsync();
        var testee = new MealTypeService(new SimpleCrudHelper(context));

        // Act
        var results = await testee.GetAllMealTypesAsync();

        // Assert
        results.Should().Contain(mealType => mealType.Name == "Lunch").And.Contain(mealType => mealType.Name == "Breakfast");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealTypeService.GetAllMealTypesAsync");
    }
}
