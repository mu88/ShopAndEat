using BizLogic.Concrete;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class GetRecipesForMealsActionTests
{
    [Test]
    public void GetRecipesForMeals()
    {
        // Arrange
        var meal1 = new MealBuilder().WithDefaults().Build();
        var meal2 = new MealBuilder().WithDefaults().Build();
        var meals = new[] { meal1, meal2 };
        var testee = new GetRecipesForMealsAction();

        // Act
        var results = testee.GetRecipesForMeals(meals);

        // Assert
        results.Should().HaveCount(2);
    }
}
