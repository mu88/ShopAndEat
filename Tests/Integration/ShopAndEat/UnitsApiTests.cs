using System.Net.Http.Json;
using DataLayer.EF;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class UnitsApiTests
{
    [Test]
    public async Task GetAll_ReturnsNonEmptyList()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.Units.Add(new EfUnit("kg"));
            context.Units.Add(new EfUnit("g"));
            context.Units.Add(new EfUnit("piece"));
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<List<string>>("shopAndEat/api/units");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("kg");
    }
}
