using BizLogic.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class GetRecipesForMealsActionTests
{
    [Test]
    public void GetRecipesForMeals()
    {
        var mealType = new MealType("Lunch", 1);
        var meal1 = new Meal(DateTime.MinValue, mealType, new Recipe("Recipe 1", 3, 2, Array.Empty<Ingredient>()), 2);
        var meal2 = new Meal(DateTime.MinValue, mealType, new Recipe("Recipe 2", 3, 2, Array.Empty<Ingredient>()), 2);
        var meals = new[] { meal1, meal2 };
        var testee = new GetRecipesForMealsAction();

        var results = testee.GetRecipesForMeals(meals);

        results.Should().HaveCount(2);
    }
}
