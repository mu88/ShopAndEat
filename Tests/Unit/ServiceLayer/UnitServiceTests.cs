using DTO.Unit;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class UnitServiceTests
{
    [Test]
    public void CreateUnit()
    {
        using var context = new InMemoryDbContext();
        var testee = new UnitService(new SimpleCrudHelper(context));
        var newUnitDto = new NewUnitDto("Piece");

        testee.CreateUnit(newUnitDto);

        context.Units.Should().Contain(unit => unit.Name == "Piece");
    }

    [Test]
    public void DeleteUnit()
    {
        using var context = new InMemoryDbContext();
        var existingUnit = context.Units.Add(new global::DataLayer.EfClasses.Unit("Piece"));
        context.SaveChanges();
        var testee = new UnitService(new SimpleCrudHelper(context));
        var deleteUnitDto = new DeleteUnitDto(existingUnit.Entity.UnitId);

        testee.DeleteUnit(deleteUnitDto);

        context.Units.Should().NotContain(unit => unit.Name == "Piece");
    }

    [Test]
    public void GetAllUnits()
    {
        using var context = new InMemoryDbContext();
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Piece"));
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Bag"));
        context.SaveChanges();
        var testee = new UnitService(new SimpleCrudHelper(context));

        var results = testee.GetAllUnits();

        results.Should().Contain(unit => unit.Name == "Piece").And.Contain(unit => unit.Name == "Bag");
    }
}
