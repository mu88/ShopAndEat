using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class IngredientDbAccessTests
{
    [Test]
    public void GetIngredient()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        var result = testee.GetIngredient(ingredient.Entity.IngredientId);

        result.Article.Name.Should().Be("Tomato");
    }

    [Test]
    public void GetIngredients()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        var result = testee.GetIngredients();

        result.Should().Contain(ingredient.Entity);
    }

    [Test]
    public void CreateIngredient()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        var result = testee.AddIngredient(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.Ingredients.Should().Contain(result);
    }

    [Test]
    public void DeleteIngredient()
    {
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article{Name="Tomato", ArticleGroup = vegetables, IsInventory = false};
        var piece = new Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        testee.DeleteIngredient(ingredient.Entity);
        inMemoryDbContext.SaveChanges();

        inMemoryDbContext.Ingredients.Should().NotContain(ingredient.Entity);
    }
}