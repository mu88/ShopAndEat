using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class IngredientDbAccessTests
{
    [Test]
    public void GetIngredient()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = testee.GetIngredient(ingredient.Entity.IngredientId);

        // Assert
        result.Article.Name.Should().Be("Tomato");
    }

    [Test]
    public void GetIngredients()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = testee.GetIngredients();

        // Assert
        result.Should().Contain(ingredient.Entity);
    }

    [Test]
    public void CreateIngredient()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = testee.AddIngredient(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Ingredients.Should().Contain(result);
    }

    [Test]
    public void DeleteIngredient()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var vegetables = new ArticleGroup("Vegetables");
        var tomato = new Article { Name = "Tomato", ArticleGroup = vegetables, IsInventory = false };
        var piece = new global::DataLayer.EfClasses.Unit("Piece");
        inMemoryDbContext.ArticleGroups.Add(vegetables);
        inMemoryDbContext.Articles.Add(tomato);
        inMemoryDbContext.Units.Add(piece);
        var ingredient = inMemoryDbContext.Ingredients.Add(new Ingredient(tomato, 2, piece));
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        testee.DeleteIngredient(ingredient.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Ingredients.Should().NotContain(ingredient.Entity);
    }
}
