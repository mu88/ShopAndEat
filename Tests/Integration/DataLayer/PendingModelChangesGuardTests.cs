using DataLayer.EF;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration.EfCoreMigrations;

/// <summary>
/// Guards the blanket <c>PendingModelChangesWarning</c> suppression in <c>Program.cs</c>. That warning
/// is ignored solely because dotnet-ef 10.0.10 reports a non-convergent "Sqlite:Autoincrement" annotation
/// diff for the strongly-typed primary keys, even immediately after a fresh migration scaffold. If the
/// compiled model ever drifts from the checked-in migrations for any OTHER reason (a genuinely missing
/// migration), this test fails loudly instead of that drift being silently swallowed in production.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PendingModelChangesGuardTests
{
    [Test]
    public void CompiledModel_HasNoPendingChanges_OtherThanTheKnownAutoincrementAnnotationDiff()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<EfCoreContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        using var context = new EfCoreContext(options);
        var services = context.GetInfrastructure();
        var migrationsAssembly = services.GetRequiredService<IMigrationsAssembly>();
        var differ = services.GetRequiredService<IMigrationsModelDiffer>();
        var runtimeInitializer = services.GetRequiredService<IModelRuntimeInitializer>();

        // The snapshot model produced by a scaffolded migration is conventions-built but not yet
        // "finalized" (runtime-initialized), which GetRelationalModel() requires; the live, compiled
        // model (IDesignTimeModel.Model) is already finalized.
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
        if (snapshotModel is IMutableModel mutableSnapshotModel)
        {
            snapshotModel = runtimeInitializer.Initialize(mutableSnapshotModel.FinalizeModel());
        }

        var currentModel = context.GetService<IDesignTimeModel>().Model;

        // Act
        var pendingOperations = differ.GetDifferences(
            snapshotModel?.GetRelationalModel(),
            currentModel.GetRelationalModel());

        // Assert — confirm the known quirk is still present (so this test isn't silently vacuous), and that
        // every single pending operation is that known altering-column-only diff caused by the dotnet-ef
        // 10.0.10 "Sqlite:Autoincrement" annotation quirk; anything else (a new/altered/removed table, index,
        // or a column change unrelated to that annotation) means a migration is genuinely missing.
        // NOTE: If/when the dotnet-ef Autoincrement quirk is fixed in a future EF Core version, this NotBeEmpty()
        // assertion will start failing — that's expected and means this guard should be re-evaluated/simplified.
        pendingOperations.Should().NotBeEmpty();
        pendingOperations.Should().OnlyContain(operation => IsKnownAutoincrementAnnotationDiff(operation));
    }

    private static bool IsKnownAutoincrementAnnotationDiff(MigrationOperation operation)
    {
        if (operation is not AlterColumnOperation alterColumnOperation)
        {
            return false;
        }

        var oldColumn = alterColumnOperation.OldColumn;
        var hasAutoincrementAnnotation = oldColumn.GetAnnotations()
            .Any(annotation => string.Equals(annotation.Name, "Sqlite:Autoincrement", StringComparison.Ordinal));

        // The known dotnet-ef 10.0.10 quirk is purely a phantom "Sqlite:Autoincrement" annotation on
        // OldColumn; every other column facet must be identical between old and new, otherwise this is
        // a real pending model change (e.g. a type or nullability drift) that must not be swallowed.
        return hasAutoincrementAnnotation
            && alterColumnOperation.ClrType == oldColumn.ClrType
            && string.Equals(alterColumnOperation.ColumnType, oldColumn.ColumnType, StringComparison.Ordinal)
            && alterColumnOperation.IsNullable == oldColumn.IsNullable
            && alterColumnOperation.IsRowVersion == oldColumn.IsRowVersion
            && alterColumnOperation.MaxLength == oldColumn.MaxLength
            && alterColumnOperation.Precision == oldColumn.Precision
            && alterColumnOperation.Scale == oldColumn.Scale
            && alterColumnOperation.IsUnicode == oldColumn.IsUnicode
            && alterColumnOperation.IsFixedLength == oldColumn.IsFixedLength
            && Equals(alterColumnOperation.DefaultValue, oldColumn.DefaultValue)
            && string.Equals(alterColumnOperation.DefaultValueSql, oldColumn.DefaultValueSql, StringComparison.Ordinal)
            && string.Equals(alterColumnOperation.ComputedColumnSql, oldColumn.ComputedColumnSql, StringComparison.Ordinal);
    }
}
