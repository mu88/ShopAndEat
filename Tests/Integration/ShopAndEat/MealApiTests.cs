using System.Net.Http.Json;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class MealApiTests
{
    [Test]
    public async Task GetMealsForToday()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        await using (var serviceScope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<EfCoreContext>();

            // Meal/MealType kept as direct construction: Day must be DateTime.Today for the "today" filter,
            // and MealType.Name/Order drive the asserted result order below. Recipe content is unasserted filler.
            context.Meals.Add(new Meal(DateTime.Today, new MealType("Breakfast", 1), new RecipeBuilder().WithDefaults().Build(), 1));
            context.Meals.Add(new Meal(DateTime.Today, new MealType("Lunch", 2), new RecipeBuilder().WithDefaults().Build(), 1));
            await context.SaveChangesAsync();
        }

        var client = webApplicationFactory.CreateClient();

        // Act
        var results = await client.GetFromJsonAsync<IEnumerable<ExistingMealDto>>("shopAndEat/api/meals/mealsForToday");

        // Assert
        results.Should()
            .HaveCount(2)
            .And.Subject.Should()
            .SatisfyRespectively(first => first.MealType.Name.Should().Be("Breakfast"),
                second => second.MealType.Name.Should().Be("Lunch"));
    }
}
