using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingAgentDiagnosticsTests
{
    [Test]
    public void ResolveVersion_PrefersInformationalVersion_WhenPresent()
    {
        // Act
        var result = ShoppingAgentDiagnostics.ResolveVersion("1.2.3-preview", "4.5.6.0");

        // Assert
        result.Should().Be("1.2.3-preview");
    }

    [Test]
    public void ResolveVersion_FallsBackToAssemblyVersion_WhenInformationalVersionIsMissing()
    {
        // Act
        var result = ShoppingAgentDiagnostics.ResolveVersion(null, "4.5.6.0");

        // Assert
        result.Should().Be("4.5.6.0");
    }

    [Test]
    public void ResolveVersion_FallsBackToDefault_WhenBothAreMissing()
    {
        // Act
        var result = ShoppingAgentDiagnostics.ResolveVersion(null, null);

        // Assert
        result.Should().Be("0.0.0");
    }
}
