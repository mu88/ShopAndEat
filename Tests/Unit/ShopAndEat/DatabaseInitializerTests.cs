using FluentAssertions;
using NUnit.Framework;
using ShopAndEat;

namespace Tests.Unit.ShopAndEat;

[TestFixture]
[Category("Unit")]
public class DatabaseInitializerTests
{
    [Test]
    public void GetDatabasePath_ShouldReturnNull_WhenConnectionStringIsNull()
    {
        // Arrange & Act
        var databasePath = DatabaseInitializer.GetDatabasePath(null);

        // Assert
        databasePath.Should().BeNull();
    }

    [Test]
    public void GetDatabasePath_ShouldStripDataSourcePrefix_WhenConnectionStringIsSet()
    {
        // Arrange & Act
        var databasePath = DatabaseInitializer.GetDatabasePath("Data Source=/tmp/shopandeat.db");

        // Assert
        databasePath.Should().Be("/tmp/shopandeat.db");
    }

    [Test]
    public void EnsureDatabaseDirectoryExists_ShouldCreateParentDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var parentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var databasePath = Path.Combine(parentDirectory, "shopandeat.db");
        Directory.Exists(parentDirectory).Should().BeFalse();

        try
        {
            // Act
            DatabaseInitializer.EnsureDatabaseDirectoryExists(databasePath);

            // Assert
            Directory.Exists(parentDirectory).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(parentDirectory, recursive: true);
        }
    }

    [Test]
    public void EnsureDatabaseDirectoryExists_ShouldNotThrow_WhenParentDirectoryAlreadyExists()
    {
        // Arrange
        var parentDirectory = Path.GetTempPath();
        var databasePath = Path.Combine(parentDirectory, "shopandeat.db");

        // Act
        var act = () => DatabaseInitializer.EnsureDatabaseDirectoryExists(databasePath);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void EnsureDatabaseDirectoryExists_ShouldNotThrow_WhenPathHasNoParentDirectory()
    {
        // Arrange
        var databasePath = Path.GetPathRoot(Path.GetTempPath())!;

        // Act
        var act = () => DatabaseInitializer.EnsureDatabaseDirectoryExists(databasePath);

        // Assert
        act.Should().NotThrow();
    }
}
