using DataLayer.EfClasses;
using DTO.MealType;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class MealTypeServiceTests
{
    [Test]
    public void CreateMealType()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var newMealTypeDto = new NewMealTypeDto("Lunch");

        // Act
        testee.CreateMealType(newMealTypeDto);

        // Assert
        context.MealTypes.Should().Contain(mealType => mealType.Name == "Lunch");
    }

    [Test]
    public void DeleteMealType()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var existingMealType = context.MealTypes.Add(new MealType("Lunch", 1));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var deleteMealTypeDto = new DeleteMealTypeDto(existingMealType.Entity.MealTypeId);

        // Act
        testee.DeleteMealType(deleteMealTypeDto);

        // Assert
        context.MealTypes.Should().NotContain(mealType => mealType.Name == "Lunch");
    }

    [Test]
    public void GetAllMealTypes()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        context.MealTypes.Add(new MealType("Lunch", 1));
        context.MealTypes.Add(new MealType("Breakfast", 2));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context));

        // Act
        var results = testee.GetAllMealTypes();

        // Assert
        results.Should().Contain(mealType => mealType.Name == "Lunch").And.Contain(mealType => mealType.Name == "Breakfast");
    }
}
