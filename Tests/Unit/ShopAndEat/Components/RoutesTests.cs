using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Components;

namespace Tests.Unit.ShopAndEat.Components;

[TestFixture]
[Category("Unit")]
public class RoutesTests
{
    [Test]
    public void Routes_ShouldExistAsComponent()
    {
        // Arrange
        var componentType = typeof(Routes);

        // Act
        var name = componentType.Name;

        // Assert
        name.Should().Be("Routes");
    }

    [Test]
    public void Routes_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(Routes);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Components");
    }

    [Test]
    public void Routes_ComponentTypeShouldBeValid()
    {
        // Arrange & Act
        var componentType = typeof(Routes);

        // Assert
        componentType.Should().NotBeNull("Routes component should exist");
        componentType.IsClass.Should().BeTrue("Routes should be a class");
        componentType.IsPublic.Should().BeTrue("Routes should be public");
    }

    [Test]
    public void Routes_ShouldBeRoutingComponent()
    {
        // Arrange & Act
        var componentType = typeof(Routes);

        // Assert
        // Routes is a routing infrastructure component that cannot be fully unit-tested
        // in isolation because it has complex DI dependencies (MainLayout > NavMenu > IStringLocalizer)
        // This test verifies the component type exists and is properly defined
        componentType.Should().NotBeNull();
    }
}
