using BizDbAccess;
using DataLayer.EF;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer;
using ShopAndEat.Features.ShoppingAgent;
using ShopAndEat.Features.ShoppingAgent.Adapters;
using ShoppingAgent.Services;

namespace Tests.Unit.ShopAndEat.Features.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingAgentFeatureTests
{
    [Test]
    public void EnableShoppingAgent_RegistersServerAdapters_ForPreferencesAndSessionServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IPreferencesRepository>());
        services.AddSingleton(Substitute.For<ISessionRepository>());
        services.AddSingleton(Substitute.For<IMealService>());
        services.AddSingleton<EfCoreContext>(new InMemoryDbContext());
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.EnableShoppingAgent(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IPreferencesService>().Should().BeOfType<ServerPreferencesAdapter>();
        provider.GetRequiredService<ISessionService>().Should().BeOfType<ServerSessionAdapter>();
    }

    [Test]
    public void EnableShoppingAgent_RegistersOpenTelemetryTracingAndMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IPreferencesRepository>());
        services.AddSingleton(Substitute.For<ISessionRepository>());
        services.AddSingleton(Substitute.For<IMealService>());
        services.AddSingleton<EfCoreContext>(new InMemoryDbContext());
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.EnableShoppingAgent(configuration);

        // Assert
        services.Should().Contain(descriptor => string.Equals(descriptor.ServiceType.Name, "TracerProvider", StringComparison.Ordinal));
        services.Should().Contain(descriptor => string.Equals(descriptor.ServiceType.Name, "MeterProvider", StringComparison.Ordinal));
    }
}
