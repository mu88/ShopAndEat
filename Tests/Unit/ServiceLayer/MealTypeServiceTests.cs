using DataLayer.EfClasses;
using DTO.MealType;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Doubles;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class MealTypeServiceTests
{
    [Test]
    public void CreateMealType()
    {
        using var context = new InMemoryDbContext();
        var testee = new MealTypeService(new SimpleCrudHelper(context, TestMapper.Create()));
        var newMealTypeDto = new NewMealTypeDto("Lunch");

        testee.CreateMealType(newMealTypeDto);

        context.MealTypes.Should().Contain(x => x.Name == "Lunch");
    }

    [Test]
    public void DeleteMealType()
    {
        using var context = new InMemoryDbContext();
        var existingMealType = context.MealTypes.Add(new MealType("Lunch", 1));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context, TestMapper.Create()));
        var deleteMealTypeDto = new DeleteMealTypeDto(existingMealType.Entity.MealTypeId);

        testee.DeleteMealType(deleteMealTypeDto);

        context.MealTypes.Should().NotContain(x => x.Name == "Lunch");
    }

    [Test]
    public void GetAllMealTypes()
    {
        using var context = new InMemoryDbContext();
        context.MealTypes.Add(new MealType("Lunch", 1));
        context.MealTypes.Add(new MealType("Breakfast", 2));
        context.SaveChanges();
        var testee = new MealTypeService(new SimpleCrudHelper(context, TestMapper.Create()));

        var results = testee.GetAllMealTypes();

        results.Should().Contain(x => x.Name == "Lunch").And.Contain(x => x.Name == "Breakfast");
    }
}
