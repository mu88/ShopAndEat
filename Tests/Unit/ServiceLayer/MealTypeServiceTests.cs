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
        using var context = new InMemoryDbContext();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var newMealTypeDto = new NewMealTypeDto("Lunch");

        testee.CreateMealType(newMealTypeDto);

        context.MealTypes.Should().Contain(mealType => mealType.Name == "Lunch");
    }

    [Test]
    public void DeleteMealType()
    {
        using var context = new InMemoryDbContext();
        var existingMealType = context.MealTypes.Add(new MealType("Lunch", 1));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context));
        var deleteMealTypeDto = new DeleteMealTypeDto(existingMealType.Entity.MealTypeId);

        testee.DeleteMealType(deleteMealTypeDto);

        context.MealTypes.Should().NotContain(mealType => mealType.Name == "Lunch");
    }

    [Test]
    public void GetAllMealTypes()
    {
        using var context = new InMemoryDbContext();
        context.MealTypes.Add(new MealType("Lunch", 1));
        context.MealTypes.Add(new MealType("Breakfast", 2));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context));

        var results = testee.GetAllMealTypes();

        results.Should().Contain(mealType => mealType.Name == "Lunch").And.Contain(mealType => mealType.Name == "Breakfast");
    }
}
