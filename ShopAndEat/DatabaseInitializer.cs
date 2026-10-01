namespace ShopAndEat;

/// <summary>
/// Pure, unit-testable helper logic extracted from Program.cs's startup DB-seeding local function,
/// so the edge cases (missing connection string, missing parent directory) can be covered without
/// booting a full <see cref="Microsoft.AspNetCore.Builder.WebApplication"/> or a real database.
/// </summary>
internal static class DatabaseInitializer
{
    public static string? GetDatabasePath(string? connectionString) =>
        connectionString?.Replace("Data Source=", string.Empty, StringComparison.Ordinal);

    public static void EnsureDatabaseDirectoryExists(string databasePath)
    {
        var parentDirectoryOfDatabase = Directory.GetParent(databasePath);
        if (parentDirectoryOfDatabase is { Exists: false })
        {
            Directory.CreateDirectory(parentDirectoryOfDatabase.FullName);
        }
    }
}
