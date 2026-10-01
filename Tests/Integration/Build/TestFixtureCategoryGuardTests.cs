using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Integration.Build;

/// <summary>
/// Guards the convention that every test fixture's <c>[Category]</c> attribute matches its
/// top-level namespace segment (<c>Tests.Unit.*</c> -&gt; <c>"Unit"</c>, <c>Tests.Integration.*</c>
/// -&gt; <c>"Integration"</c>, <c>Tests.System.*</c> -&gt; <c>"System"</c>, <c>Tests.LlmIntegration.*</c>
/// -&gt; <c>"LlmIntegration"</c>). This matters most for <c>Tests.Unit.*</c>: Stryker's
/// <c>test-case-filter</c> in stryker-config.json is <c>"TestCategory=Unit"</c>, so a fixture under
/// Tests/Unit missing <c>[Category("Unit")]</c> would silently be excluded from mutation testing -
/// it would still pass under a plain `dotnet test` run, hiding the gap.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TestFixtureCategoryGuardTests
{
    [Test]
    public void EveryTestFixture_HasACategoryMatchingItsTopLevelNamespaceSegment()
    {
        // Arrange
        var testFixtures = typeof(TestFixtureCategoryGuardTests).Assembly
            .GetTypes()
            .Where(type => Attribute.IsDefined(type, typeof(TestFixtureAttribute)))
            .ToList();

        // This guards against the guard itself silently becoming a no-op (e.g. if test discovery
        // were ever broken or this ran against the wrong assembly).
        testFixtures.Should().NotBeEmpty();

        // Act
        var fixturesWithMismatchedOrMissingCategory = testFixtures
            .Select(type => new
            {
                Type = type,
                ExpectedCategory = GetTopLevelNamespaceSegment(type),
                ActualCategories = type.GetCustomAttributes<CategoryAttribute>().Select(attribute => attribute.Name).ToList(),
            })
            .Where(fixture => !fixture.ActualCategories.Contains(fixture.ExpectedCategory, StringComparer.Ordinal))
            .Select(fixture => $"{fixture.Type.FullName} expected [Category(\"{fixture.ExpectedCategory}\")] but has [{string.Join(", ", fixture.ActualCategories)}]")
            .ToList();

        // Assert
        fixturesWithMismatchedOrMissingCategory.Should().BeEmpty(
            "every test fixture's [Category] must match its top-level namespace segment (Tests.Unit.* -> \"Unit\", etc.), otherwise it silently falls out of Stryker's \"TestCategory=Unit\" mutation-testing filter or other category-based test selection");
    }

    private static string GetTopLevelNamespaceSegment(Type type)
    {
        // "Tests.Unit.ServiceLayer" -> "Unit"; "Tests.LlmIntegration" -> "LlmIntegration".
        var segments = type.Namespace!.Split('.');
        return segments[1];
    }
}
