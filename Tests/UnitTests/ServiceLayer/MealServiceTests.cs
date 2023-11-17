using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Ingredient;
using DTO.Meal;
using DTO.MealType;
using DTO.Recipe;
using FluentAssertions;
using FluentAssertions.Extensions;
using Moq;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Doubles;

namespace Tests.UnitTests.ServiceLayer;

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
                                                              Enumerable.Empty<ExistingIngredientDto>(),
                                                              lunchRecipe.RecipeId),
                                        1,
                                        2);
        var testee = CreateTestee(context);

        // Act
        testee.CreateMeal(newMealDto);

        // Assert
        context.Meals.Should().HaveCount(2)
            .And.Subject.Select(meal => meal.Day).Should().BeEquivalentTo(new []{4.November(2023), 5.November(2023)});
    }

    private static MealService CreateTestee(EfCoreContext context)
    {
        var mapper = TestMapper.Create();
        var testee = new MealService(Mock.Of<IGeneratePurchaseItemsForRecipesAction>(),
                                     Mock.Of<IOrderPurchaseItemsByStoreAction>(),
                                     Mock.Of<IGetRecipesForMealsAction>(),
                                     context,
                                     new SimpleCrudHelper(context, mapper),
                                     mapper);
        return testee;
    }
}