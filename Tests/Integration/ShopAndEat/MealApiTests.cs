using System.Net.Http.Json;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class MealApiTests
{
    [Test]
    public async Task GetMealsForToday()
    {
        // Arrange
        var webApplicationFactory = new CustomWebApplicationFactory();
        using (var serviceScope = webApplicationFactory.Services.CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.Meals.Add(new Meal(DateTime.Today, new MealType("Breakfast", 1), new Recipe("My breakfast", 2, 2, Enumerable.Empty<Ingredient>()), 1));
            context.Meals.Add(new Meal(DateTime.Today, new MealType("Lunch", 2), new Recipe("My lunch", 2, 2, Enumerable.Empty<Ingredient>()), 1));
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