using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.IngredientList;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

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

        var articleGroup = new ArticleGroup("Vegetables");
        var shoppingOrder = new ShoppingOrder(articleGroup, 1);
        var store = new Store("Test Store", new[] { shoppingOrder });
        var unit = new DataLayer.EfClasses.Unit("kg");
        var article = new Article { Name = "Tomato", ArticleGroup = articleGroup };
        var ingredient = new Ingredient(article, 2, unit);
        var recipe = new Recipe("Test Recipe", 1, 2, new[] { ingredient });
        var mealType = new MealType("Lunch", 1);
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

        var articleGroup = new ArticleGroup("Vegetables");
        var shoppingOrder = new ShoppingOrder(articleGroup, 1);
        var store = new Store("Test Store", new[] { shoppingOrder });
        var unit = new DataLayer.EfClasses.Unit("kg");
        var article = new Article { Name = "Tomato", ArticleGroup = articleGroup };
        var ingredient = new Ingredient(article, 2, unit);
        var recipe = new Recipe("Test Recipe", 1, 2, new[] { ingredient });
        var mealType = new MealType("Lunch", 1);
        var meal = new Meal(DateTime.Today, mealType, recipe, 2);

        int storeId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.Stores.Add(store);
            context.Meals.Add(meal);
            await context.SaveChangesAsync();
            storeId = store.StoreId;
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
