using System.Diagnostics;
using System.Reflection;

namespace ShoppingAgent.Diagnostics;

public static class ShoppingAgentDiagnostics
{
    public const string ActivitySourceName = "ShoppingAgent";
    public const string MeterName = "ShoppingAgent";

    private static readonly string Version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
}
