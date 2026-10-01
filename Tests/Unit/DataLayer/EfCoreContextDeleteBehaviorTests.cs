using DataLayer.EfClasses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Unit.DataLayer;

/// <summary>
/// Regression tests proving that the FK relationships EF Core would otherwise default to
/// ON DELETE CASCADE (because the navigation is required/non-nullable) are explicitly configured
/// as ON DELETE RESTRICT in <see cref="DataLayer.EF.EfCoreContext"/>. Deleting a "reference" entity
/// (ArticleGroup, Article, Unit, MealType) while it is still in use must fail loudly instead of
/// silently wiping out dependent rows. Like
/// <c>DeleteRecipeAsync_WithRealDatabaseCascade_DeletesMealsViaDbConstraint</c> in
/// RecipeServiceTests, these use a real Sqlite database and clear the change tracker before
/// acting, so the constraint is enforced by the database itself, not EF's client-side fixup.
/// </summary>
[TestFixture]
[Category("Unit")]
public class EfCoreContextDeleteBehaviorTests
{
    [Test]
    public async Task DeletingArticleGroup_WhileReferencedByArticle_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        context.Articles.Add(new Article("Tomato", articleGroup.Entity));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedArticleGroup = await context.ArticleGroups.SingleAsync(g => g.ArticleGroupId == articleGroup.Entity.ArticleGroupId);
        context.ArticleGroups.Remove(trackedArticleGroup);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task DeletingArticleGroup_WhileReferencedByShoppingOrder_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        context.ShoppingOrders.Add(new ShoppingOrder(articleGroup.Entity, 1));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedArticleGroup = await context.ArticleGroups.SingleAsync(g => g.ArticleGroupId == articleGroup.Entity.ArticleGroupId);
        context.ArticleGroups.Remove(trackedArticleGroup);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task DeletingArticle_WhileReferencedByIngredient_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        var article = context.Articles.Add(new Article("Tomato", articleGroup.Entity));
        var unit = context.Units.Add(new EfUnit("kg"));
        await context.SaveChangesAsync();
        var recipe = context.Recipes.Add(new Recipe("Salad", 1, 2, [new Ingredient(article.Entity, 2, unit.Entity)]));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedArticle = await context.Articles.SingleAsync(a => a.ArticleId == article.Entity.ArticleId);
        context.Articles.Remove(trackedArticle);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        recipe.Entity.Should().NotBeNull(); // keeps the `recipe` local meaningfully used
    }

    [Test]
    public async Task DeletingUnit_WhileReferencedByIngredient_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        var article = context.Articles.Add(new Article("Tomato", articleGroup.Entity));
        var unit = context.Units.Add(new EfUnit("kg"));
        await context.SaveChangesAsync();
        context.Recipes.Add(new Recipe("Salad", 1, 2, [new Ingredient(article.Entity, 2, unit.Entity)]));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedUnit = await context.Units.SingleAsync(u => u.UnitId == unit.Entity.UnitId);
        context.Units.Remove(trackedUnit);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task DeletingArticle_WhileReferencedByPurchaseItem_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        var article = context.Articles.Add(new Article("Tomato", articleGroup.Entity));
        var unit = context.Units.Add(new EfUnit("kg"));
        await context.SaveChangesAsync();
        var purchase = context.Purchases.Add(new Purchase(DateTime.Today, DateTime.Today, [new PurchaseItem(article.Entity, 2, unit.Entity)]));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedArticle = await context.Articles.SingleAsync(a => a.ArticleId == article.Entity.ArticleId);
        context.Articles.Remove(trackedArticle);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        purchase.Entity.Should().NotBeNull(); // keeps the `purchase` local meaningfully used
    }

    [Test]
    public async Task DeletingUnit_WhileReferencedByPurchaseItem_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        await context.SaveChangesAsync();
        var article = context.Articles.Add(new Article("Tomato", articleGroup.Entity));
        var unit = context.Units.Add(new EfUnit("kg"));
        await context.SaveChangesAsync();
        context.Purchases.Add(new Purchase(DateTime.Today, DateTime.Today, [new PurchaseItem(article.Entity, 2, unit.Entity)]));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedUnit = await context.Units.SingleAsync(u => u.UnitId == unit.Entity.UnitId);
        context.Units.Remove(trackedUnit);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task DeletingMealType_WhileReferencedByMeal_ThrowsDueToRestrictedForeignKey()
    {
        // Arrange
        await using var context = new SqliteDbContext();
        var mealType = context.MealTypes.Add(new MealType("Dinner", 1));
        await context.SaveChangesAsync();
        var recipe = context.Recipes.Add(new Recipe("Omelette", 1, 2, Enumerable.Empty<Ingredient>()));
        await context.SaveChangesAsync();
        context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, recipe.Entity, 2));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var trackedMealType = await context.MealTypes.SingleAsync(mt => mt.MealTypeId == mealType.Entity.MealTypeId);
        context.MealTypes.Remove(trackedMealType);

        // Act
        var act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
