using DataLayer.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests;

/// <summary>
/// An <see cref="EfCoreContext"/> backed by a real (in-memory) Sqlite database instead of the EF Core
/// InMemory provider. Use this when a test relies on relational query translation (e.g. ORDER BY on a
/// value-converted strongly-typed key), which the InMemory provider cannot evaluate client-side because
/// those key types intentionally do not implement <see cref="IComparable"/>.
/// </summary>
public sealed class SqliteDbContext : EfCoreContext, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContext()
        : base(CreateOptions(out var connection))
    {
        _connection = connection;
        Database.EnsureCreated();
    }

    private static DbContextOptions<EfCoreContext> CreateOptions(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        return new DbContextOptionsBuilder<EfCoreContext>().UseSqlite(connection).Options;
    }

    public new void Dispose()
    {
        _connection.Dispose();
        base.Dispose();
    }
}
