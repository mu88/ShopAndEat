using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class UnitTests
{
    [Test]
    public void CreateUnit()
    {
        var name = "Liter";

        var testee = new Unit(name);

        testee.Name.Should().Be(name);
    }
}