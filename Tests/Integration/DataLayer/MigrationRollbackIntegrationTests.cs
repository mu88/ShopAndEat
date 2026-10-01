using DataLayer.EF;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration.Migrations;

/// <summary>
/// Exercises every migration's <c>Down()</c> method by migrating a real SQLite database up to the
/// latest migration and then rolling it all the way back down. <c>Down()</c> is never invoked by the
/// app itself (<c>Program.cs</c> only ever calls <c>Migrate()</c> forward), so no other test reaches it.
/// </summary>
[TestFixture]
[Category("Integration")]
public class MigrationRollbackIntegrationTests
{
    [Test]
    public async Task Migrator_ShouldRollBackAllMigrations_WithoutThrowing()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<EfCoreContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = new EfCoreContext(options);
        var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();
        await context.Database.MigrateAsync();

        // Act
        var act = async () => await migrator.MigrateAsync(Migration.InitialDatabase);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
