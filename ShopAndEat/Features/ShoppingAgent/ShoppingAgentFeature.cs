using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using ShopAndEat.Features.ShoppingAgent.Adapters;
using ShoppingAgent;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Services;

namespace ShopAndEat.Features.ShoppingAgent;

public static class ShoppingAgentFeature
{
    /// <summary>
    /// Activates the ShoppingAgent feature: registers all agent services,
    /// wires up server-side adapters for preferences and sessions,
    /// and extends the OpenTelemetry pipeline with ShoppingAgent traces and metrics.
    /// </summary>
    public static IServiceCollection EnableShoppingAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddShoppingAgentCore(configuration, adapters =>
        {
            adapters.AddScoped<IPreferencesService, ServerPreferencesAdapter>();
            adapters.AddScoped<ISessionService, ServerSessionAdapter>();
        });

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(ShoppingAgentDiagnostics.ActivitySourceName))
            .WithMetrics(metrics => metrics.AddMeter(ShoppingAgentDiagnostics.MeterName));

        return services;
    }
}
