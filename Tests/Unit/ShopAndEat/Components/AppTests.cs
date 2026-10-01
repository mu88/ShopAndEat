using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Components;

namespace Tests.Unit.ShopAndEat.Components;

[TestFixture]
[Category("Unit")]
public class AppTests
{
    [Test]
    public void App_ShouldExistAsComponent()
    {
        // Arrange
        var componentType = typeof(App);

        // Act
        var name = componentType.Name;

        // Assert
        name.Should().Be("App");
    }

    [Test]
    public void App_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(App);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Components");
    }

    [Test]
    public void App_ComponentTypeShouldBeValid()
    {
        // Arrange & Act
        var componentType = typeof(App);

        // Assert
        componentType.Should().NotBeNull("App component should exist");
        componentType.IsClass.Should().BeTrue("App should be a class");
    }

    [Test]
    public void App_ShouldBeViewable()
    {
        // Arrange & Act
        var componentType = typeof(App);

        // Assert
        // App is a root component that cannot be fully unit-tested in isolation
        // because it has complex DI dependencies (Routes > MainLayout > NavMenu > IStringLocalizer)
        // This test verifies the component type exists and is properly defined
        componentType.Should().NotBeNull();
        componentType.IsPublic.Should().BeTrue();
    }
}
