using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Services;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class PreferenceDtoTests
{
    [Test]
    public void PreferenceDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PreferenceDto();

        // Assert
        dto.Scope.Should().Be(string.Empty);
        dto.Key.Should().Be(string.Empty);
        dto.Value.Should().Be(string.Empty);
        dto.StoreKey.Should().BeNull();
    }

    [Test]
    public void PreferenceDto_CanSetAllProperties()
    {
        // Arrange & Act
        var dto = new PreferenceDto
        {
            Scope = "article:Tofu",
            Key = "confirmed_product",
            Value = "Organic Tofu 200g",
            StoreKey = "coop",
        };

        // Assert
        dto.Scope.Should().Be("article:Tofu");
        dto.Key.Should().Be("confirmed_product");
        dto.Value.Should().Be("Organic Tofu 200g");
        dto.StoreKey.Should().Be("coop");
    }

    [Test]
    public void PreferenceDto_EqualityByValue()
    {
        // Arrange
        var first = new PreferenceDto { Scope = "global", Key = "prefer_bio", Value = "true", StoreKey = null };
        var second = new PreferenceDto { Scope = "global", Key = "prefer_bio", Value = "true", StoreKey = null };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void PreferenceDto_Inequality_WhenDifferentScope()
    {
        // Arrange
        var first = new PreferenceDto { Scope = "global" };
        var second = new PreferenceDto { Scope = "article:Milk" };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Test]
    public void PreferenceDto_ToString_ContainsTypeName()
    {
        // Arrange
        var dto = new PreferenceDto { Scope = "global", Key = "k" };

        // Act
        var result = dto.ToString();

        // Assert
        result.Should().Contain("PreferenceDto");
    }

    [Test]
    public void PreferenceDto_GetHashCode_EqualForEqualValues()
    {
        // Arrange
        var first = new PreferenceDto { Scope = "s", Key = "k", Value = "v", StoreKey = "sk" };
        var second = new PreferenceDto { Scope = "s", Key = "k", Value = "v", StoreKey = "sk" };

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
