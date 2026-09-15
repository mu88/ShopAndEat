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
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class MealServiceTests
{
    [Test]
    public void GetMealsForToday()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var mealType1 = new MealType("Breakfast", 1);
        var mealType2 = new MealType("Lunch", 2);
        context.Meals.AddRange(new Meal(DateTime.Today.AddDays(-1), mealType1, new Recipe("My breakfast", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today, mealType1, new Recipe("My breakfast", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today, mealType2, new Recipe("My lunch", 2, 2, Enumerable.Empty<Ingredient>()), 1),
            new Meal(DateTime.Today.AddDays(1), mealType2, new Recipe("My lunch", 2, 2, Enumerable.Empty<Ingredient>()), 1));
        context.SaveChanges();
        var testee = CreateTestee(context);

        // Act
        var results = testee.GetMealsForToday();

        // Assert
        results.Should()
            .HaveCount(2)
            .And.Subject.Should()
            .AllSatisfy(meal => meal.Day.Should().BeSameDateAs(DateTime.Today))
            .And.Subject.Should()
            .SatisfyRespectively(first => first.MealType.Name.Should().Be("Breakfast"),
                second => second.MealType.Name.Should().Be("Lunch"));
    }

    [Test]
    public void GetFutureMeals()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var lunch = new MealType("Lunch", 1);
        var lunchRecipe = new Recipe("My lunch", 1, 1, Enumerable.Empty<Ingredient>());
        context.Meals.AddRange(new Meal(DateTime.Today.AddDays(-1), lunch, lunchRecipe, 1),
            new Meal(DateTime.Today, lunch, lunchRecipe, 1),
            new Meal(DateTime.Today.AddDays(1), lunch, lunchRecipe, 1));
        context.SaveChanges();
        var testee = CreateTestee(context);

        // Act
        var results = testee.GetFutureMeals();

        // Assert
        results.Should()
            .HaveCount(2)
            .And.Subject.Should()
            .SatisfyRespectively(first => first.Day.Should().BeSameDateAs(DateTime.Today),
                second => second.Day.Should().BeSameDateAs(DateTime.Today.AddDays(1)));
    }

    [Test]
    public void CreateMeal_ShouldCreateMealsForSeveralDays()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var lunch = new MealType("Lunch", 1);
        var lunchRecipe = new Recipe("My lunch", 1, 1, Enumerable.Empty<Ingredient>());
        context.MealTypes.Add(lunch);
        context.Recipes.Add(lunchRecipe);
        context.SaveChanges();
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
        testee.CreateMeal(newMealDto);

        // Assert
        context.Meals.Should()
            .HaveCount(2)
            .And.Subject.Select(meal => meal.Day)
            .Should()
            .BeEquivalentTo(new[] { 4.November(2023), 5.November(2023) });
    }

    [Test]
    public void GetOrderedPurchaseItems_ShouldIgnorePastMeals()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var today = new DateTime(2026, 9, 15);
        var vegetables = new ArticleGroup("Vegetables");
        var store = new Store("Test Store", new[] { new ShoppingOrder(vegetables, 1) });
        var unit = new global::DataLayer.EfClasses.Unit("Piece");
        var pastArticle = new Article { Name = "Past Tomato", ArticleGroup = vegetables };
        var futureArticle = new Article { Name = "Future Salad", ArticleGroup = vegetables };
        var mealType = new MealType("Lunch", 1);
        var pastMeal = new Meal(today.AddDays(-1), mealType, new Recipe("Past Recipe", 1, 1, new[] { new Ingredient(pastArticle, 1, unit) }), 1);
        var futureMeal = new Meal(today, mealType, new Recipe("Future Recipe", 1, 1, new[] { new Ingredient(futureArticle, 2, unit) }), 1);
        context.Stores.Add(store);
        context.Meals.AddRange(pastMeal, futureMeal);
        context.SaveChanges();
        var testee = CreateTesteeWithRealPurchaseItemActions(context, new FixedTimeProvider(today));

        // Act
        var results = testee.GetOrderedPurchaseItems(new ExistingStoreDto(store.StoreId, store.Name)).ToList();

        // Assert
        results.Should().ContainSingle();
        results.Single().Article.Name.Should().Be("Future Salad");
        pastMeal.HasBeenShopped.Should().BeFalse();
        futureMeal.HasBeenShopped.Should().BeTrue();
    }

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
