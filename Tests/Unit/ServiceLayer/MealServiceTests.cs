using System.Diagnostics;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Ingredient;
using DTO.Meal;
using DTO.MealType;
using DTO.Recipe;
using DTO.Store;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class MealServiceTests
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
    public async Task GetMealsForTodayAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var mealType1 = new MealType("Breakfast", 1);
        var mealType2 = new MealType("Lunch", 2);
        context.Meals.AddRange(new Meal(DateTime.Today.AddDays(-1), mealType1, new Recipe("My breakfast", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today, mealType1, new Recipe("My breakfast", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today, mealType2, new Recipe("My lunch", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today.AddDays(1), mealType2, new Recipe("My lunch", 2, 2, Enumerable.Empty<Ingredient>()), 1));
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = await testee.GetMealsForTodayAsync();

        // Assert
        results.Should()
            .HaveCount(2)
            .And.Subject.Should()
            .AllSatisfy(meal => meal.Day.Should().BeSameDateAs(DateTime.Today))
            .And.Subject.Should()
            .SatisfyRespectively(first => first.MealType.Name.Should().Be("Breakfast"),
                second => second.MealType.Name.Should().Be("Lunch"));
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.GetMealsForTodayAsync");
    }

    [Test]
    public async Task GetFutureMealsAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var lunch = new MealType("Lunch", 1);
        var breakfast = new MealType("Breakfast", 0);
        var lunchRecipe = new Recipe("My lunch", 1, 1, Enumerable.Empty<Ingredient>());
        var breakfastRecipe = new Recipe("My breakfast", 1, 1, Enumerable.Empty<Ingredient>());
        context.Meals.AddRange(new Meal(DateTime.Today.AddDays(-1), lunch, lunchRecipe, 1),
            new Meal(DateTime.Today, lunch, lunchRecipe, 1),
            // Same future day, deliberately added in descending MealType.Order to prove ThenBy() sorts ascending.
            new Meal(DateTime.Today.AddDays(1), lunch, lunchRecipe, 1),
            new Meal(DateTime.Today.AddDays(1), breakfast, breakfastRecipe, 1));
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = await testee.GetFutureMealsAsync();

        // Assert
        results.Should()
            .HaveCount(3)
            .And.Subject.Should()
            .SatisfyRespectively(first => first.Day.Should().BeSameDateAs(DateTime.Today),
                second =>
                {
                    second.Day.Should().BeSameDateAs(DateTime.Today.AddDays(1));
                    second.MealType.Name.Should().Be("Breakfast");
                },
                third =>
                {
                    third.Day.Should().BeSameDateAs(DateTime.Today.AddDays(1));
                    third.MealType.Name.Should().Be("Lunch");
                });
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.GetFutureMealsAsync");
    }

    [Test]
    public async Task CreateMealAsync_ShouldCreateMealsForSeveralDays()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var lunch = new MealTypeBuilder().WithDefaults().Build();
        var lunchRecipe = new RecipeBuilder().WithDefaults().Build();
        context.MealTypes.Add(lunch);
        context.Recipes.Add(lunchRecipe);
        await context.SaveChangesAsync();
        var newMealDto = new NewMealDto(4.November(2023),
            new ExistingMealTypeDto(lunch.Name, lunch.MealTypeId, lunch.Order),
            new ExistingRecipeDto(lunchRecipe.Name,
                lunchRecipe.NumberOfDays,
                lunchRecipe.NumberOfPersons,
                [],
                lunchRecipe.RecipeId),
            1,
            2);
        var testee = CreateTestee(context);

        // Act
        await testee.CreateMealAsync(newMealDto);

        // Assert
        context.Meals.Should()
            .HaveCount(2)
            .And.Subject.Select(meal => meal.Day)
            .Should()
            .BeEquivalentTo(new[] { 4.November(2023), 5.November(2023) });
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.CreateMealAsync");
    }

    [Test]
    public async Task GetOrderedPurchaseItemsAsync_ShouldIgnorePastMeals()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var today = new DateTime(2026, 9, 15);
        var vegetables = new ArticleGroupBuilder().WithDefaults().Build();
        // Store/Article kept as direct construction: they must share this exact "vegetables" instance
        // for the ArticleGroup reference-equality match in OrderPurchaseItemsByStoreAction to succeed.
        var store = new Store("Test Store", new[] { new ShoppingOrder(vegetables, 1) });
        var unit = new UnitBuilder().WithDefaults().Build();
        var pastArticle = new Article("Past Tomato", vegetables);
        var futureArticle = new Article("Future Salad", vegetables); // kept: article names are asserted on below
        var mealType = new MealTypeBuilder().WithDefaults().Build();
        var pastMeal = new Meal(today.AddDays(-1), mealType, new Recipe("Past Recipe", 1, 1, new[] { new Ingredient(pastArticle, 1, unit) }), 1);
        var futureMeal = new Meal(today, mealType, new Recipe("Future Recipe", 1, 1, new[] { new Ingredient(futureArticle, 2, unit) }), 1);
        context.Stores.Add(store);
        context.Meals.AddRange(pastMeal, futureMeal);
        await context.SaveChangesAsync();
        var testee = CreateTesteeWithRealPurchaseItemActions(context, new FixedTimeProvider(today));

        // Act
        var results = (await testee.GetOrderedPurchaseItemsAsync(new ExistingStoreDto(store.StoreId, store.Name))).ToList();

        // Assert
        results.Should().ContainSingle();
        results.Single().Article.Name.Should().Be("Future Salad");
        pastMeal.HasBeenShopped.Should().BeFalse();
        futureMeal.HasBeenShopped.Should().BeTrue();
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.GetOrderedPurchaseItemsAsync");
    }

    [Test]
    public async Task GetOrderedPurchaseItemsAsync_ShouldPersistHasBeenShoppedFlag()
    {
        // Arrange — a second context on the same underlying in-memory database proves SaveChangesAsync() ran,
        // since the first context's change tracker would otherwise mask the missing persistence.
        var dbOptions = CreateSharedDbOptions();
        await using var writeContext = new EfCoreContext(dbOptions);
        var today = new DateTime(2026, 9, 15);
        var vegetables = new ArticleGroupBuilder().WithDefaults().Build();
        // Store/Article kept as direct construction: they must share this exact "vegetables" instance
        // for the ArticleGroup reference-equality match in OrderPurchaseItemsByStoreAction to succeed.
        var store = writeContext.Stores.Add(new Store("Test Store", new[] { new ShoppingOrder(vegetables, 1) })).Entity;
        var unit = new UnitBuilder().WithDefaults().Build();
        var futureArticle = new Article("Future Salad", vegetables);
        var mealType = new MealTypeBuilder().WithDefaults().Build();
        var futureMeal = writeContext.Meals.Add(
            new Meal(today, mealType, new Recipe("Future Recipe", 1, 1, new[] { new Ingredient(futureArticle, 2, unit) }), 1)).Entity;
        await writeContext.SaveChangesAsync();
        var testee = CreateTesteeWithRealPurchaseItemActions(writeContext, new FixedTimeProvider(today));

        // Act
        await testee.GetOrderedPurchaseItemsAsync(new ExistingStoreDto(store.StoreId, store.Name));

        // Assert
        await using var readContext = new EfCoreContext(dbOptions);
        readContext.Meals.Single(meal => meal.MealId == futureMeal.MealId).HasBeenShopped.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMealAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var meal = context.Meals.Add(new MealBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteMealAsync(new DeleteMealDto(meal.MealId));

        // Assert
        context.Meals.Should().NotContain(m => m.MealId == meal.MealId);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.DeleteMealAsync");
    }

    [Test]
    public async Task DeleteMealAsync_ShouldPersistDeletion()
    {
        // Arrange — a second context on the same underlying in-memory database proves SaveChangesAsync() ran.
        var dbOptions = CreateSharedDbOptions();
        await using var writeContext = new EfCoreContext(dbOptions);
        var meal = writeContext.Meals.Add(new MealBuilder().WithDefaults().Build()).Entity;
        await writeContext.SaveChangesAsync();
        var testee = CreateTestee(writeContext);

        // Act
        await testee.DeleteMealAsync(new DeleteMealDto(meal.MealId));

        // Assert
        await using var readContext = new EfCoreContext(dbOptions);
        readContext.Meals.Should().NotContain(m => m.MealId == meal.MealId);
    }

    [Test]
    public async Task ToggleMealAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var meal = context.Meals.Add(new MealBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        await testee.ToggleMealAsync(meal.MealId.Value);

        // Assert
        context.Meals.Single(m => m.MealId == meal.MealId).HasBeenShopped.Should().BeTrue();
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "MealService.ToggleMealAsync");
    }

    [Test]
    public async Task ToggleMealAsync_ShouldPersistToggle()
    {
        // Arrange — a second context on the same underlying in-memory database proves SaveChangesAsync() ran.
        var dbOptions = CreateSharedDbOptions();
        await using var writeContext = new EfCoreContext(dbOptions);
        var meal = writeContext.Meals.Add(new MealBuilder().WithDefaults().Build()).Entity;
        await writeContext.SaveChangesAsync();
        var testee = CreateTestee(writeContext);

        // Act
        await testee.ToggleMealAsync(meal.MealId.Value);

        // Assert
        await using var readContext = new EfCoreContext(dbOptions);
        readContext.Meals.Single(m => m.MealId == meal.MealId).HasBeenShopped.Should().BeTrue();
    }

    private static DbContextOptions<EfCoreContext> CreateSharedDbOptions() =>
        new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static MealService CreateTestee(EfCoreContext context)
    {
        var testee = new MealService(Substitute.For<IGeneratePurchaseItemsForRecipesAction>(),
            Substitute.For<IOrderPurchaseItemsByStoreAction>(),
            Substitute.For<IGetRecipesForMealsAction>(),
            context,
            new SimpleCrudHelper(context),
            TimeProvider.System);
        return testee;
    }

    private static MealService CreateTesteeWithRealPurchaseItemActions(EfCoreContext context, TimeProvider timeProvider)
        => new(new GeneratePurchaseItemsForRecipesAction(),
            new OrderPurchaseItemsByStoreAction(),
            new GetRecipesForMealsAction(),
            context,
            new SimpleCrudHelper(context),
            timeProvider);

    private sealed class FixedTimeProvider(DateTime today) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(today, TimeSpan.Zero);
    }
}
