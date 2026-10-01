using DataLayer.EF;
using DataLayer.EfClasses;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

// NOTE: deliberately NOT "Tests.Integration.DataLayer" (matching the folder) despite living in
// Tests/Integration/DataLayer/: with C# file-scoped namespaces, a nested namespace literally named
// "DataLayer" shadows the real top-level DataLayer namespace for every `using DataLayer.X;` directive
// in sibling Tests.Integration.* files, breaking unrelated compilation. See PendingModelChangesGuardTests.cs
// in this same folder, which hit exactly this problem.
namespace Tests.Integration.EfCoreMigrations;

/// <summary>
/// Guards Finding 5's <c>IHasId&lt;TId&gt;</c> marker interface against silently drifting from the
/// entity's real EF Core primary key: <c>IHasId&lt;TId&gt;</c> has no members, so e.g.
/// <c>Article : IHasId&lt;StoreId&gt;</c> would compile fine despite being wrong, defeating the whole
/// point of constraining <c>SimpleCrudHelper</c>'s generics by key type.
/// </summary>
[TestFixture]
[Category("Integration")]
public class IHasIdPrimaryKeyConsistencyTests
{
    [Test]
    public void EveryIHasIdImplementation_DeclaresTheSameClrTypeAsItsActualEfCorePrimaryKey()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<EfCoreContext>().UseSqlite(connection).Options;
        using var context = new EfCoreContext(options);
        var model = context.Model;

        var entitiesImplementingIHasId = typeof(EfCoreContext).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => (Type: type, HasIdInterface: type.GetInterfaces()
                .FirstOrDefault(implementedInterface =>
                    implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof(IHasId<>))))
            .Where(entity => entity.HasIdInterface is not null)
            .ToList();

        // This guards against the guard itself silently becoming a no-op (e.g. if IHasId were removed
        // from every entity by accident).
        entitiesImplementingIHasId.Should().NotBeEmpty();

        // Act & Assert
        foreach (var (entityType, hasIdInterface) in entitiesImplementingIHasId)
        {
            var declaredIdType = hasIdInterface!.GetGenericArguments()[0];
            var efEntityType = model.FindEntityType(entityType);
            efEntityType.Should().NotBeNull($"{entityType.Name} implements IHasId<{declaredIdType.Name}> and should be mapped as an EF Core entity");

            var primaryKeyProperties = efEntityType!.FindPrimaryKey()?.Properties ?? [];
            primaryKeyProperties.Should().HaveCount(1, $"{entityType.Name}'s IHasId<TId> assumes a single-column primary key");
            primaryKeyProperties[0].ClrType.Should().Be(
                declaredIdType,
                $"{entityType.Name} implements IHasId<{declaredIdType.Name}>, but its actual EF Core primary key is of a different type");
        }
    }
}
