using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class SimpleCrudHelperTests
{
    [Test]
    public async Task FindAsync_WithStronglyTypedId_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existingUnit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = new SimpleCrudHelper(context);

        // Act
        var result = await testee.FindAsync<global::DataLayer.EfClasses.Unit>(existingUnit.Entity.UnitId);

        // Assert
        result.Should().BeSameAs(existingUnit.Entity);
    }

    [Test]
    public void FindAsync_WithStronglyTypedId_WhenEntityDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new SimpleCrudHelper(context);
        var missingUnitId = new UnitId(-1);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await testee.FindAsync<global::DataLayer.EfClasses.Unit>(missingUnitId));
    }

    [Test]
    public async Task FindAsync_WithIntId_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var recipe = context.Recipes.Add(new RecipeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var existingIngredient = recipe.Entity.Ingredients.Single();
        var testee = new SimpleCrudHelper(context);

        // Act
        var result = await testee.FindAsync<Ingredient>(existingIngredient.IngredientId);

        // Assert
        result.Should().BeSameAs(existingIngredient);
    }

    [Test]
    public void FindAsync_WithIntId_WhenEntityDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new SimpleCrudHelper(context);

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await testee.FindAsync<Ingredient>(-1));
        exception!.Message.Should().Be("No Ingredient entity found for key '-1'.");
    }

    [Test]
    public async Task DeleteAsync_WithStronglyTypedId_WhenEntityExists_DeletesEntity()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existingUnit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = new SimpleCrudHelper(context);

        // Act
        await testee.DeleteAsync<global::DataLayer.EfClasses.Unit>(existingUnit.Entity.UnitId);

        // Assert
        context.Units.Should().NotContain(existingUnit.Entity);
    }

    [Test]
    public void DeleteAsync_WithStronglyTypedId_WhenEntityDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new SimpleCrudHelper(context);
        var missingUnitId = new UnitId(-1);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await testee.DeleteAsync<global::DataLayer.EfClasses.Unit>(missingUnitId));
    }

    [Test]
    public void DeleteAsync_WithIntId_WhenEntityDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new SimpleCrudHelper(context);

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await testee.DeleteAsync<Ingredient>(-1));
        exception!.Message.Should().Be("No Ingredient entity found for key '-1'.");
    }
}
