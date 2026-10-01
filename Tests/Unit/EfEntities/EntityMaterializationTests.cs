using System.Reflection;
using DataLayer.EF;
using DataLayer.EfClasses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Unit.EfEntities;

/// <summary>
/// These tests force EF Core to materialize entities from a fresh <see cref="EfCoreContext"/> instance
/// (instead of returning already-tracked instances from the same context). This is the only way the
/// private/protected constructors and navigation-property setters, which exist solely for EF Core
/// materialization (marked with <c>[UsedImplicitly]</c>), get exercised.
/// </summary>
[TestFixture]
[Category("Unit")]
public class EntityMaterializationTests
{
    [Test]
    public async Task Entities_ShouldMaterializeViaEfCorePrivateMembers_WhenLoadedFromFreshContext()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using (var writeContext = CreateContext(dbName))
        {
            var articleGroup = writeContext.ArticleGroups.Add(new ArticleGroup("Vegetables")).Entity;
            var article = writeContext.Articles.Add(new Article("Tomato", articleGroup)).Entity;
            var unit = writeContext.Units.Add(new EfUnit("Piece")).Entity;
            var mealType = writeContext.MealTypes.Add(new MealType("Dinner", order: 1)).Entity;
            var ingredient = writeContext.Ingredients.Add(new Ingredient(article, quantity: 2, unit)).Entity;
            var recipe = writeContext.Recipes.Add(new Recipe("Salad", numberOfDays: 1, numberOfPersons: 2, [ingredient])).Entity;
            writeContext.Meals.Add(new Meal(DateTime.Today, mealType, recipe, numberOfPersons: 2));
            writeContext.Purchases.Add(new Purchase(DateTime.Today, DateTime.Today, [new PurchaseItem(article, 3, unit)]));
            var shoppingOrder = writeContext.ShoppingOrders.Add(new ShoppingOrder(articleGroup, order: 1)).Entity;
            writeContext.Stores.Add(new Store("Coop", [shoppingOrder]));
            var session = writeContext.ShoppingSessions.Add(new ShoppingSession("500g carrots", DateTimeOffset.UtcNow)).Entity;
            writeContext.ShoppingSessionItems.Add(new ShoppingSessionItem("500g carrots", session.ShoppingSessionId, DateTimeOffset.UtcNow));

            await writeContext.SaveChangesAsync();
        }

        // Act
        await using var readContext = CreateContext(dbName);
        var loadedArticle = await readContext.Articles.Include(a => a.ArticleGroup).SingleAsync();
        var loadedIngredient = await readContext.Ingredients.Include(i => i.Article).Include(i => i.Unit).SingleAsync();
        var loadedRecipe = await readContext.Recipes.Include(r => r.Ingredients).SingleAsync();
        var loadedMeal = await readContext.Meals.Include(m => m.MealType).Include(m => m.Recipe).SingleAsync();
        var loadedPurchase = await readContext.Purchases.Include(p => p.PurchaseItems).SingleAsync();
        var loadedPurchaseItem = await readContext.PurchaseItems.Include(pi => pi.Article).Include(pi => pi.Unit).SingleAsync();
        var loadedShoppingOrder = await readContext.ShoppingOrders.Include(o => o.ArticleGroup).SingleAsync();
        var loadedSessionItem = await readContext.ShoppingSessionItems.SingleAsync();

        // Assert
        loadedArticle.ArticleGroup.Name.Should().Be("Vegetables");
        loadedIngredient.Article.Name.Should().Be("Tomato");
        loadedIngredient.Unit.Name.Should().Be("Piece");
        loadedRecipe.Ingredients.Should().ContainSingle();
        loadedMeal.MealType.Name.Should().Be("Dinner");
        loadedMeal.Recipe.Name.Should().Be("Salad");
        loadedPurchase.PurchaseItems.Should().ContainSingle();
        loadedPurchaseItem.Article.Name.Should().Be("Tomato");
        loadedPurchaseItem.Unit.Name.Should().Be("Piece");
        loadedShoppingOrder.ArticleGroup.Name.Should().Be("Vegetables");
        loadedSessionItem.OriginalIngredient.Should().Be("500g carrots");
    }

    [Test]
    public void IdTypes_ShouldFormatValueAsInvariantString_WhenCallingToString()
    {
        // Arrange & Act & Assert
        new ArticleGroupId(1).ToString().Should().Be("1");
        new ArticleId(2).ToString().Should().Be("2");
        new MealId(3).ToString().Should().Be("3");
        new OnlineArticleMappingId(4).ToString().Should().Be("4");
        new RecipeId(5).ToString().Should().Be("5");
        new ShoppingPreferenceId(6).ToString().Should().Be("6");
        new ShoppingSessionId(7).ToString().Should().Be("7");
        new ShoppingSessionItemId(8).ToString().Should().Be("8");
        new StoreId(9).ToString().Should().Be("9");
        new UnitId(10).ToString().Should().Be("10");
    }

    [Test]
    public void NavigationSetters_ShouldAssignBackingField_WhenInvokedViaReflection()
    {
        // Arrange
        // EF Core uses PropertyAccessMode.PreferField for these navigation properties (a matching backing
        // field exists), so it never calls the property setter itself during materialization. The setters
        // still need to exist and work correctly (e.g. for future callers or a changed EF configuration),
        // so they are exercised directly via reflection here.
        var articleGroup = new ArticleGroup("Vegetables");
        var otherArticleGroup = new ArticleGroup("Fruits");
        var article = new Article("Tomato", articleGroup);
        var unit = new EfUnit("Piece");
        var otherUnit = new EfUnit("Gram");
        var ingredient = new Ingredient(article, quantity: 1, unit);
        var mealType = new MealType("Dinner", order: 1);
        var otherMealType = new MealType("Lunch", order: 2);
        var recipe = new Recipe("Salad", numberOfDays: 1, numberOfPersons: 2, [ingredient]);
        var otherRecipe = new Recipe("Soup", numberOfDays: 1, numberOfPersons: 2, [ingredient]);
        var meal = new Meal(DateTime.Today, mealType, recipe, numberOfPersons: 2);
        var purchaseItem = new PurchaseItem(article, quantity: 1, unit);
        var otherIngredients = new List<Ingredient> { ingredient };
        var shoppingOrder = new ShoppingOrder(articleGroup, order: 1);

        // Act
        SetPrivateProperty(article, nameof(Article.ArticleGroup), otherArticleGroup);
        SetPrivateProperty(ingredient, nameof(Ingredient.Article), article);
        SetPrivateProperty(ingredient, nameof(Ingredient.Unit), otherUnit);
        SetPrivateProperty(meal, nameof(Meal.MealType), otherMealType);
        SetPrivateProperty(meal, nameof(Meal.Recipe), otherRecipe);
        SetPrivateProperty(purchaseItem, nameof(PurchaseItem.Article), article);
        SetPrivateProperty(purchaseItem, nameof(PurchaseItem.Unit), otherUnit);
        SetPrivateProperty(recipe, nameof(Recipe.Ingredients), otherIngredients);
        SetPrivateProperty(shoppingOrder, nameof(ShoppingOrder.ArticleGroup), otherArticleGroup);

        // Assert
        article.ArticleGroup.Should().BeSameAs(otherArticleGroup);
        ingredient.Article.Should().BeSameAs(article);
        ingredient.Unit.Should().BeSameAs(otherUnit);
        meal.MealType.Should().BeSameAs(otherMealType);
        meal.Recipe.Should().BeSameAs(otherRecipe);
        purchaseItem.Article.Should().BeSameAs(article);
        purchaseItem.Unit.Should().BeSameAs(otherUnit);
        recipe.Ingredients.Should().BeSameAs(otherIngredients);
        shoppingOrder.ArticleGroup.Should().BeSameAs(otherArticleGroup);
    }

    private static EfCoreContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(dbName).Options);

    private static void SetPrivateProperty<TEntity, TValue>(TEntity entity, string propertyName, TValue value)
    {
        var property = typeof(TEntity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, value);
    }
}
