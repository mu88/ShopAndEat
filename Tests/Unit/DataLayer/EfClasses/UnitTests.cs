using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class UnitTests
{
    [Test]
    public void CreateUnit()
    {
        // Arrange
        var name = "Liter";

        // Act
        var testee = new global::DataLayer.EfClasses.Unit(name);

        // Assert
        testee.Name.Should().Be(name);
    }
}
