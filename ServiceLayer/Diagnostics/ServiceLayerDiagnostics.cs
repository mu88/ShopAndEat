using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ServiceLayer.Diagnostics;

/// <summary>
/// <see cref="ActivitySource"/> for OpenTelemetry tracing of the legacy (pre-ShoppingAgent-era)
/// Article/Meal/Recipe/Store/Unit business services in <c>ServiceLayer.Concrete</c>.
/// </summary>
public static class ServiceLayerDiagnostics
{
    public const string ActivitySourceName = "ShopAndEat.ServiceLayer";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, GetVersion());

    /// <summary>
    /// Resolves the version string for OpenTelemetry tracing, preferring the informational version
    /// (e.g. from GitVersion) and falling back to the assembly version, then a hardcoded default.
    /// </summary>
    internal static string ResolveVersion(string? informationalVersion, string? assemblyVersion) =>
        informationalVersion ?? assemblyVersion ?? "0.0.0";

    // Both AssemblyInformationalVersionAttribute and the assembly version are always present at build time,
    // so only one branch of ResolveVersion is ever reachable here; the fallback logic itself is unit-tested in isolation.
    [ExcludeFromCodeCoverage]
    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return ResolveVersion(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            assembly.GetName().Version?.ToString());
    }
}
