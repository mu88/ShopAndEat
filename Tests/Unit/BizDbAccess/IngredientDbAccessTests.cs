using BizDbAccess.Concrete;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class IngredientDbAccessTests
{
    [Test]
    public async Task GetIngredientAsync()
    {
        // Arrange
        await using var inMemoryDbContext = new InMemoryDbContext();
        var ingredient = new IngredientBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(ingredient.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(ingredient.Article);
        inMemoryDbContext.Units.Add(ingredient.Unit);
        var ingredientEntry = inMemoryDbContext.Ingredients.Add(ingredient);
        await inMemoryDbContext.SaveChangesAsync();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = await testee.GetIngredientAsync(ingredientEntry.Entity.IngredientId);

        // Assert
        result.Article.Name.Should().Be(ingredient.Article.Name);
    }

    [Test]
    public async Task GetIngredientsAsync()
    {
        // Arrange
        await using var inMemoryDbContext = new InMemoryDbContext();
        var ingredient = new IngredientBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(ingredient.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(ingredient.Article);
        inMemoryDbContext.Units.Add(ingredient.Unit);
        var ingredientEntry = inMemoryDbContext.Ingredients.Add(ingredient);
        await inMemoryDbContext.SaveChangesAsync();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = await testee.GetIngredientsAsync();

        // Assert
        result.Should().Contain(ingredientEntry.Entity);
    }

    [Test]
    public void CreateIngredient()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var ingredient = new IngredientBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(ingredient.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(ingredient.Article);
        inMemoryDbContext.Units.Add(ingredient.Unit);
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        var result = testee.AddIngredient(ingredient);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Ingredients.Should().Contain(result);
    }

    [Test]
    public void DeleteIngredient()
    {
        // Arrange
        using var inMemoryDbContext = new InMemoryDbContext();
        var ingredient = new IngredientBuilder().WithDefaults().Build();
        inMemoryDbContext.ArticleGroups.Add(ingredient.Article.ArticleGroup);
        inMemoryDbContext.Articles.Add(ingredient.Article);
        inMemoryDbContext.Units.Add(ingredient.Unit);
        var ingredientEntry = inMemoryDbContext.Ingredients.Add(ingredient);
        inMemoryDbContext.SaveChanges();
        var testee = new IngredientDbAccess(inMemoryDbContext);

        // Act
        testee.DeleteIngredient(ingredientEntry.Entity);
        inMemoryDbContext.SaveChanges();

        // Assert
        inMemoryDbContext.Ingredients.Should().NotContain(ingredientEntry.Entity);
    }
}
