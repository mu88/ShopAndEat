using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class MealTypeTests
{
    [Test]
    public void CreateMealType()
    {
        // Arrange
        var name = "Lunch";

        // Act
        var testee = new MealType(name, 1);

        // Assert
        testee.Name.Should().Be(name);
    }
}
