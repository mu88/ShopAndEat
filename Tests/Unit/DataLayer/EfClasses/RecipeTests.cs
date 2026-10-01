using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class RecipeTests
{
    [Test]
    public void CreateRecipe()
    {
        // Arrange
        var name = "Suppe";
        var numberOfDays = 3;
        var ingredients = new Collection<Ingredient>
        {
            new(new Article("Tomato", new ArticleGroup("Vegetables"), isInventory: false),
                3,
                new global::DataLayer.EfClasses.Unit("Bag"))
        };

        // Act
        var testee = new Recipe(name, numberOfDays, 2, ingredients);

        // Assert
        testee.Name.Should().Be(name);
        testee.NumberOfDays.Should().Be(numberOfDays);
        testee.Ingredients.Should().BeEquivalentTo(ingredients);
        testee.NumberOfDays.Should().Be(3);
    }

    [Test]
    public void DefaultConstructor_SetsNameToEmptyStringAndIngredientsToEmptyList()
    {
        // Act
        var testee = new Recipe();

        // Assert
        testee.Name.Should().Be(string.Empty);
        testee.Ingredients.Should().BeEmpty();
    }
}
