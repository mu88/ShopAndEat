using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class IngredientTests
{
    [Test]
    public void CreateIngredient()
    {
        var article = new Article { Name = "Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false };
        uint quantity = 3;
        var unit = new global::DataLayer.EfClasses.Unit("Bag");

        var testee = new Ingredient(article, quantity, unit);

        testee.Article.Should().Be(article);
        testee.Quantity.Should().Be(quantity);
        testee.Unit.Should().Be(unit);
    }
}