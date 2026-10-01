using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Integration.Build;

/// <summary>
/// Guards <c>coverage.settings.xml</c>'s <c>ModulePaths/Include</c> allowlist (Finding m4) against
/// silently drifting from <c>ShopAndEat.slnx</c>'s actual production project list: because the
/// allowlist is a deliberate, hand-maintained regex list (not a convention-based pattern - see the
/// MAINTENANCE comment in coverage.settings.xml), a new production project added to the solution
/// without a matching ModulePath entry would silently get 0% coverage reporting instead of
/// appearing as a visible gap.
/// </summary>
[TestFixture]
[Category("Integration")]
public partial class CoverageSettingsModulePathsTests
{
    [Test]
    public void ModulePathsAllowlist_MatchesSolutionsProductionProjects_WhenComparedToSlnx()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var productionProjectNames = ExtractProductionProjectNames(repositoryRoot);
        var moduleAllowlistNames = ExtractModuleAllowlistNames(repositoryRoot);

        // This guards against the guard itself silently becoming a no-op (e.g. if the .slnx were
        // ever emptied or misparsed).
        productionProjectNames.Should().NotBeEmpty();

        // Act & Assert
        moduleAllowlistNames.Should().BeEquivalentTo(
            productionProjectNames,
            "coverage.settings.xml's ModulePaths/Include allowlist must cover exactly the production projects declared in ShopAndEat.slnx (excluding Tests.csproj itself) - add a matching ModulePath entry whenever a new production project is added to the solution");
    }

    private static List<string> ExtractProductionProjectNames(string repositoryRoot)
    {
        var slnxPath = Path.Combine(repositoryRoot, "ShopAndEat.slnx");
        var slnx = XDocument.Load(slnxPath);

        return slnx.Descendants("Project")
            .Select(project => project.Attribute("Path")!.Value)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(projectName => !string.Equals(projectName, "Tests", StringComparison.Ordinal))
            .Select(projectName => projectName!)
            .ToList();
    }

    private static List<string> ExtractModuleAllowlistNames(string repositoryRoot)
    {
        var coverageSettingsPath = Path.Combine(repositoryRoot, "coverage.settings.xml");
        var coverageSettings = XDocument.Load(coverageSettingsPath);

        return coverageSettings.Descendants("ModulePath")
            .Select(modulePath => modulePath.Value)
            .Select(pattern => ModulePathAssemblyNameRegex.Match(pattern))
            .Select(match =>
            {
                match.Success.Should().BeTrue("every ModulePath entry is expected to match the '.*<AssemblyName>\\.dll$' pattern");
                return match.Groups["assemblyName"].Value;
            })
            .ToList();
    }

    [GeneratedRegex(@"(?<assemblyName>[A-Za-z0-9]+)\\\.dll\$$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ModulePathAssemblyNameRegex { get; }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ShopAndEat.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root containing ShopAndEat.slnx should be discoverable by walking up from the test output directory");
        return directory!.FullName;
    }
}
