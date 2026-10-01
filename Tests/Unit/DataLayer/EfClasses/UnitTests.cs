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

    [Test]
    public void DefaultConstructor_SetsNameToEmptyString()
    {
        // Act
        var testee = new global::DataLayer.EfClasses.Unit();

        // Assert
        testee.Name.Should().Be(string.Empty);
    }
}
