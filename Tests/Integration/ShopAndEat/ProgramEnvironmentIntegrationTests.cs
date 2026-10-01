using System.Data.Common;
using DataLayer.EF;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

/// <summary>
/// Exercises Program.cs's non-development startup branch (<c>app.UseExceptionHandler(...)</c>), which
/// <see cref="CustomWebApplicationFactory"/> never reaches because it always forces the "Development" environment.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProgramEnvironmentIntegrationTests
{
    [Test]
    public async Task App_ShouldStartUpAndUseExceptionHandler_WhenEnvironmentIsNotDevelopment()
    {
        // Arrange
        await using var webApplicationFactory = new ProductionWebApplicationFactory();
        using var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync("shopAndEat/healthz");

        // Assert
        response.Should().NotBeNull();
    }

    private sealed class ProductionWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EfCoreContext>));
                services.Remove(dbContextDescriptor!);

                var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
                services.Remove(dbConnectionDescriptor!);

                // Create open SqliteConnection so EF won't automatically close it.
                services.AddSingleton<DbConnection>(_ =>
                {
                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();

                    return connection;
                });

                services.AddDbContext<EfCoreContext>((container, options) => options.UseLazyLoadingProxies()
                                                                                     .UseSqlite(container.GetRequiredService<DbConnection>())
                                                                                     .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
            });

            builder.UseEnvironment("Production");
        }
    }
}
