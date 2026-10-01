using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ShoppingPreferenceTests
{
    [Test]
    public void CreateShoppingPreference()
    {
        // Arrange
        var scope = "global";
        var key = "prefer_bio";
        var source = PreferenceSource.UserConfirmed;
        var storeKey = "coop";

        // Act
        var testee = new ShoppingPreference(scope, key, source, storeKey);

        // Assert
        testee.Scope.Should().Be(scope);
        testee.Key.Should().Be(key);
        testee.Source.Should().Be(source);
        testee.StoreKey.Should().Be(storeKey);
    }

    [Test]
    public void DefaultConstructor_SetsStringPropertiesToEmptyString()
    {
        // Act — EF materializes instances via the protected parameterless constructor.
        var testee = (ShoppingPreference)Activator.CreateInstance(typeof(ShoppingPreference), nonPublic: true)!;

        // Assert
        testee.Scope.Should().Be(string.Empty);
        testee.Key.Should().Be(string.Empty);
    }
}
