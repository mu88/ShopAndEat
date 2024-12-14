using BizLogic.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class GenerateIngredientsForRecipesActionTests
{
    [Test]
    public void GenerateComponents()
    {
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var salad = new Article { Name = "Salad", ArticleGroup = vegetables, IsInventory = false };
        var bag = new global::DataLayer.EfClasses.Unit("Bag");
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        var ingredient1 = new Ingredient(tomato, 2, bag);
        var ingredient2 = new Ingredient(salad, 3, bag);
        var ingredient3 = new Ingredient(tomato, 1, piece);
        var ingredient4 = new Ingredient(salad, 5, bag);
        var recipe1 = new Recipe("Bla", 1, 2, new[] { ingredient1, ingredient2 });
        var recipe2 = new Recipe("Blub", 1, 2, new[] { ingredient3, ingredient4 });
        var testee = new GeneratePurchaseItemsForRecipesAction();

        var results = testee.GeneratePurchaseItems(new[] { (recipe1, 2), (recipe2, 2) }).ToList();

        results.Should().HaveCount(3);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 2 && x.Unit == bag);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 1 && x.Unit == piece);
        results.Should().Contain(x => x.Article == salad && x.Quantity == 8 && x.Unit == bag);
    }

    [Test]
    public void GenerateComponentsWithDifferentNumberOfPersons()
    {
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var salad = new Article { Name = "Salad", ArticleGroup = vegetables, IsInventory = false };
        var bag = new global::DataLayer.EfClasses.Unit("Bag");
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        var ingredient1 = new Ingredient(tomato, 2, bag);
        var ingredient2 = new Ingredient(salad, 3, bag);
        var ingredient3 = new Ingredient(tomato, 1, piece);
        var ingredient4 = new Ingredient(salad, 5, bag);
        var recipe1 = new Recipe("Recipe1", 1, 2, new[] { ingredient1, ingredient2 });
        var recipe2 = new Recipe("Recipe2", 1, 1, new[] { ingredient3, ingredient4 });
        var testee = new GeneratePurchaseItemsForRecipesAction();

        var results = testee.GeneratePurchaseItems(new[] { (recipe1, 2), (recipe2, 2) }).ToList();

        results.Should().HaveCount(3);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 2 && x.Unit == bag);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 2 && x.Unit == piece);
        results.Should().Contain(x => x.Article == salad && x.Quantity == 13 && x.Unit == bag);
    }

    [Test]
    public void GenerateComponentsWithDifferentNumberOfDays()
    {
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var salad = new Article { Name = "Salad", ArticleGroup = vegetables, IsInventory = false };
        var bag = new global::DataLayer.EfClasses.Unit("Bag");
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        var ingredient1 = new Ingredient(tomato, 4, bag);
        var ingredient2 = new Ingredient(salad, 3, bag);
        var ingredient3 = new Ingredient(tomato, 1, piece);
        var ingredient4 = new Ingredient(salad, 5, bag);
        var recipe1 = new Recipe("Recipe1", 1, 2, new[] { ingredient1, ingredient2 });
        var recipe2 = new Recipe("Recipe2", 2, 2, new[] { ingredient3, ingredient4 });
        var testee = new GeneratePurchaseItemsForRecipesAction();

        var results = testee.GeneratePurchaseItems(new[] { (recipe1, 2), (recipe2, 2) }).ToList();

        results.Should().HaveCount(3);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 4 && x.Unit == bag);
        results.Should().Contain(x => x.Article == tomato && x.Quantity == 0.5 && x.Unit == piece);
        results.Should().Contain(x => x.Article == salad && x.Quantity == 5.5 && x.Unit == bag);
    }
}