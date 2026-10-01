using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class MealTests
{
    [Test]
    public void CreateMeal()
    {
        // Arrange
        var day = DateTime.MinValue;
        var mealType = new MealType("Lunch", 1);
        var recipe = new Recipe("Soup", 3, 2, Array.Empty<Ingredient>());

        // Act
        var testee = new Meal(day, mealType, recipe, 2);

        // Assert
        testee.Day.Should().Be(day);
        testee.MealType.Should().Be(mealType);
        testee.Recipe.Should().Be(recipe);
        testee.NumberOfPersons.Should().Be(2);
        testee.HasBeenShopped.Should().BeFalse();
    }

    [Test]
    public void MarkAsShopped_SetsHasBeenShoppedToTrue()
    {
        // Arrange
        var testee = new Meal(DateTime.MinValue, new MealType("Lunch", 1), new Recipe("Soup", 3, 2, Array.Empty<Ingredient>()), 2);

        // Act
        testee.MarkAsShopped();

        // Assert
        testee.HasBeenShopped.Should().BeTrue();
    }

    [Test]
    public void MarkAsShopped_WhenAlreadyShopped_RemainsTrue()
    {
        // Arrange
        var testee = new Meal(DateTime.MinValue, new MealType("Lunch", 1), new Recipe("Soup", 3, 2, Array.Empty<Ingredient>()), 2);
        testee.MarkAsShopped();

        // Act
        testee.MarkAsShopped();

        // Assert
        testee.HasBeenShopped.Should().BeTrue();
    }

    [Test]
    public void ToggleShopped_WhenNotShopped_SetsHasBeenShoppedToTrue()
    {
        // Arrange
        var testee = new Meal(DateTime.MinValue, new MealType("Lunch", 1), new Recipe("Soup", 3, 2, Array.Empty<Ingredient>()), 2);

        // Act
        testee.ToggleShopped();

        // Assert
        testee.HasBeenShopped.Should().BeTrue();
    }

    [Test]
    public void ToggleShopped_WhenAlreadyShopped_SetsHasBeenShoppedToFalse()
    {
        // Arrange
        var testee = new Meal(DateTime.MinValue, new MealType("Lunch", 1), new Recipe("Soup", 3, 2, Array.Empty<Ingredient>()), 2);
        testee.MarkAsShopped();

        // Act
        testee.ToggleShopped();

        // Assert
        testee.HasBeenShopped.Should().BeFalse();
    }
}
