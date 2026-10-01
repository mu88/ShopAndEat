using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

/// <summary>
/// Exercises Program.cs's <c>CreateDbIfNotExists</c> local function against a real, file-based SQLite
/// database (unlike <see cref="CustomWebApplicationFactory"/>, which swaps in an in-memory database and
/// therefore never reaches the file-based happy/catch paths).
/// </summary>
[TestFixture]
[Category("Integration")]
public class DatabaseInitializationIntegrationTests
{
    [Test]
    public async Task CreateDbIfNotExists_ShouldCreateDirectoryAndMigrateDatabase_WhenDatabaseFileDoesNotExist()
    {
        // Arrange
        var databaseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var databasePath = Path.Combine(databaseDirectory, "shopandeat.db");
        Directory.Exists(databaseDirectory).Should().BeFalse();

        try
        {
            // Act
            await using (var webApplicationFactory = new FileBasedSqliteWebApplicationFactory(databasePath))
            {
                using var client = webApplicationFactory.CreateClient();
                var response = await client.GetAsync("shopAndEat/healthz");

                // Assert
                response.EnsureSuccessStatusCode();
            }

            File.Exists(databasePath).Should().BeTrue();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(databaseDirectory, recursive: true);
        }
    }

    [Test]
    public async Task CreateDbIfNotExists_ShouldFailStartupFast_WhenDatabaseFileIsCorrupt()
    {
        // Arrange — this app is DB-bound and has no meaningful fallback if its schema can't be
        // migrated, so a broken/incompatible database must fail startup loudly instead of serving
        // health checks as green while every real query would later fail against a stale schema.
        var databaseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var databasePath = Path.Combine(databaseDirectory, "shopandeat.db");
        Directory.CreateDirectory(databaseDirectory);
        await File.WriteAllTextAsync(databasePath, "not a valid SQLite database file");

        try
        {
            // Act
            var act = () => new FileBasedSqliteWebApplicationFactory(databasePath).CreateClient();

            // Assert
            act.Should().Throw<Exception>();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(databaseDirectory, recursive: true);
        }
    }

    [Test]
    public async Task CreateDbIfNotExists_ShouldReturnEarly_WhenNoConnectionStringIsConfigured()
    {
        // Arrange & Act - the app must still start up even without a resolvable connection string
        // (Database.GetConnectionString() returning null short-circuits CreateDbIfNotExists before
        // any file-system or migration work happens).
        await using var webApplicationFactory = new NullConnectionStringWebApplicationFactory();
        using var client = webApplicationFactory.CreateClient();
        var response = await client.GetAsync("shopAndEat/healthz");

        // Assert
        response.Should().NotBeNull();
    }

    private sealed class FileBasedSqliteWebApplicationFactory(string databasePath) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:SQLite", $"Data Source={databasePath}");
            builder.UseEnvironment("Development");
        }
    }

    private sealed class NullConnectionStringWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:SQLite", null);
            builder.UseEnvironment("Development");
        }
    }
}
