using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Models;

namespace Tests.Unit.ShopAndEat.Models;

[TestFixture]
[Category("Unit")]
public class RecipeModelTests
{
    [Test]
    public void Defaults_WhenNotConfigured_HasEmptyName()
    {
        // Arrange
        var testee = new RecipeModel();

        // Assert
        testee.Name.Should().BeEmpty();
    }
}
