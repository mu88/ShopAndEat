using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ArticleTests
{
    [Test]
    public void CreateArticle()
    {
        // Arrange
        var name = "Salad";
        var ingredientGroup = new ArticleGroup("Vegetables");

        // Act
        var testee = new Article { Name = name, ArticleGroup = ingredientGroup, IsInventory = true };

        // Assert
        testee.Name.Should().Be(name);
        testee.ArticleGroup.Should().Be(ingredientGroup);
        testee.IsInventory.Should().Be(true);
    }
}
