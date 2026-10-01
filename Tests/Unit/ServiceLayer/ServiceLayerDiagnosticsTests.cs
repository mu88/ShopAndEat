using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Diagnostics;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class ServiceLayerDiagnosticsTests
{
    [Test]
    public void ResolveVersion_PrefersInformationalVersion_WhenPresent()
    {
        // Act
        var result = ServiceLayerDiagnostics.ResolveVersion("1.2.3-preview", "4.5.6.0");

        // Assert
        result.Should().Be("1.2.3-preview");
    }

    [Test]
    public void ResolveVersion_FallsBackToAssemblyVersion_WhenInformationalVersionIsMissing()
    {
        // Act
        var result = ServiceLayerDiagnostics.ResolveVersion(null, "4.5.6.0");

        // Assert
        result.Should().Be("4.5.6.0");
    }

    [Test]
    public void ResolveVersion_FallsBackToDefault_WhenBothAreMissing()
    {
        // Act
        var result = ServiceLayerDiagnostics.ResolveVersion(null, null);

        // Assert
        result.Should().Be("0.0.0");
    }
}
