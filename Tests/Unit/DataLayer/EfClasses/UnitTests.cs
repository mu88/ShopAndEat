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
        var name = "Liter";

        var testee = new global::DataLayer.EfClasses.Unit(name);

        testee.Name.Should().Be(name);
    }
}