using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.IngredientList;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class IngredientListApiTests
{
    private const string BasePath = "shopAndEat/api/shopping/ingredients";

    [Test]
    public async Task GetIngredientList_ReturnsEmptyList_WhenNoStoreExists()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<IngredientListResponse>(BasePath);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Test]
    public async Task GetIngredientList_WithStoreId_Returns404_WhenStoreNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{BasePath}?storeId=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetIngredientList_ReturnsItems_WhenStoreAndMealsExist()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();

        // articleGroup is kept as a shared builder-built instance (not ArticleBuilder's own internal default),
        // because the store lookup matches compartments to articles by ArticleGroup reference identity.
        var articleGroup = new ArticleGroupBuilder().WithDefaults().Build();
        // ShoppingOrder/Store kept as direct construction: must reference the shared articleGroup above,
        // which the builders cannot accept as an injected dependency.
        var shoppingOrder = new ShoppingOrder(articleGroup, 1);
        var store = new Store("Test Store", new[] { shoppingOrder });
        // Unit kept as direct construction: "kg" is asserted below (builder default is "Piece").
        var unit = new DataLayer.EfClasses.Unit("kg");
        // Article kept as direct construction: "Tomato" is asserted below, and it must share articleGroup above.
        var article = new Article("Tomato", articleGroup);
        // Ingredient kept as direct construction: Quantity=2 is asserted below, and it must carry article/unit above.
        var ingredient = new Ingredient(article, 2, unit);
        // Recipe kept as direct construction: must carry the specific ingredient above through to the result.
        var recipe = new Recipe("Test Recipe", 1, 2, new[] { ingredient });
        // MealType is unasserted filler here.
        var mealType = new MealTypeBuilder().WithDefaults().Build();
        // Meal kept as direct construction: Day must be DateTime.Today for the service's "unshopped, due" filter.
        var meal = new Meal(DateTime.Today, mealType, recipe, 2);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.Stores.Add(store);
            context.Meals.Add(meal);
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<IngredientListResponse>(BasePath);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        var item = result.Items.First();
        item.Article.Should().Be("Tomato");
        item.Quantity.Should().Be(2);
        item.Unit.Should().Be("kg");
    }

    [Test]
    public async Task GetIngredientList_WithStoreId_ReturnsItems_WhenStoreExists()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();

        // articleGroup is kept as a shared builder-built instance (not ArticleBuilder's own internal default),
        // because the store lookup matches compartments to articles by ArticleGroup reference identity.
        var articleGroup = new ArticleGroupBuilder().WithDefaults().Build();
        // ShoppingOrder/Store kept as direct construction: must reference the shared articleGroup above,
        // which the builders cannot accept as an injected dependency.
        var shoppingOrder = new ShoppingOrder(articleGroup, 1);
        var store = new Store("Test Store", new[] { shoppingOrder });
        // Unit kept as direct construction: "kg" is asserted below (builder default is "Piece").
        var unit = new DataLayer.EfClasses.Unit("kg");
        // Article kept as direct construction: "Tomato" is asserted below, and it must share articleGroup above.
        var article = new Article("Tomato", articleGroup);
        // Ingredient kept as direct construction: Quantity=2 is asserted below, and it must carry article/unit above.
        var ingredient = new Ingredient(article, 2, unit);
        // Recipe kept as direct construction: must carry the specific ingredient above through to the result.
        var recipe = new Recipe("Test Recipe", 1, 2, new[] { ingredient });
        // MealType is unasserted filler here.
        var mealType = new MealTypeBuilder().WithDefaults().Build();
        // Meal kept as direct construction: Day must be DateTime.Today for the service's "unshopped, due" filter.
        var meal = new Meal(DateTime.Today, mealType, recipe, 2);

        int storeId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.Stores.Add(store);
            context.Meals.Add(meal);
            await context.SaveChangesAsync();
            storeId = store.StoreId.Value;
        }

        var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<IngredientListResponse>($"{BasePath}?storeId={storeId.ToString(CultureInfo.InvariantCulture)}");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        var item = result.Items.First();
        item.Article.Should().Be("Tomato");
        item.Quantity.Should().Be(2);
        item.Unit.Should().Be("kg");
    }
}
