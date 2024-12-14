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
        var name = "Suppe";
        var numberOfDays = 3;
        var ingredients = new Collection<Ingredient>
        {
            new(new Article { Name = "Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false },
                3,
                new global::DataLayer.EfClasses.Unit("Bag"))
        };

        var testee = new Recipe(name, numberOfDays, 2, ingredients);

        testee.Name.Should().Be(name);
        testee.NumberOfDays.Should().Be(numberOfDays);
        testee.Ingredients.Should().BeEquivalentTo(ingredients);
        testee.NumberOfDays.Should().Be(3);
    }
}