using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ShopAndEat.Api;

namespace Tests.Unit.ShopAndEat.Api;

[TestFixture]
[Category("Unit")]
public class UnitsControllerTests
{
    [Test]
    public async Task GetUnits_ReturnsUnitNames_OrderedByName()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Kilogramm"));
        context.Units.Add(new global::DataLayer.EfClasses.Unit("Gramm"));
        await context.SaveChangesAsync();
        var testee = new UnitsController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetUnits(testee.HttpContext.RequestAborted);

        // Assert
        result.Value.Should().Equal("Gramm", "Kilogramm");
    }
}
