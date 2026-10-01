using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent;
using ShoppingAgent.Options;
using ShoppingAgent.Services;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingAgentExtensionsTests
{
    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{LlmClientOptions.SectionName}:ApiKey"] = "test-key",
            [$"{AgentOptions.SectionName}:MaxToolCallingIterations"] = "5",
            [$"{ExtensionOptions.SectionName}:ToolCallTimeoutSeconds"] = "42",
            [$"{ShopOptions.SectionName}:Shops:0:Key"] = "coop",
            [$"{ShopOptions.SectionName}:Shops:0:Name"] = "Coop",
            [$"{ShopOptions.SectionName}:Shops:0:BaseUrl"] = "https://coop.example.test",
            [$"{ShopOptions.SectionName}:Shops:0:CartUrl"] = "https://coop.example.test/cart",
        })
        .Build();

    [Test]
    public async Task AddShoppingAgentCore_RegistersAllCoreServices_SoTheyAreAllResolvable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        var configuration = CreateConfiguration();

        // Act
        services.AddShoppingAgentCore(configuration, adapters =>
        {
            adapters.AddScoped<IPreferencesService>(_ => Substitute.For<IPreferencesService>());
            adapters.AddScoped<ISessionService>(_ => Substitute.For<ISessionService>());
        });
        await using var provider = services.BuildServiceProvider();

        // Assert — resolving the top-level orchestrator forces the whole DI graph (all AddScoped/
        // AddSingleton/AddHttpClient registrations below) to be built; a removed registration line
        // (Stryker statement-mutation) would make this throw. The scope is disposed asynchronously
        // because ExtensionBridge only implements IAsyncDisposable.
        await using var scope = provider.CreateAsyncScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        agentService.Should().NotBeNull();
        // TimeProvider.System is registered standalone (not injected into any other resolved
        // service above), so it must be asserted explicitly to kill its removal mutant.
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    [Test]
    public void AddShoppingAgentCore_BindsOptionsSectionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        var configuration = CreateConfiguration();

        // Act
        services.AddShoppingAgentCore(configuration, adapters =>
        {
            adapters.AddScoped<IPreferencesService>(_ => Substitute.For<IPreferencesService>());
            adapters.AddScoped<ISessionService>(_ => Substitute.For<ISessionService>());
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmClientOptions>>().Value.ApiKey.Should().Be("test-key");
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentOptions>>().Value.MaxToolCallingIterations.Should().Be(5);
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExtensionOptions>>().Value.ToolCallTimeoutSeconds.Should().Be(42);
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ShopOptions>>().Value.Shops.Should().ContainSingle(shop => shop.Key == "coop");
    }

    [Test]
    public void AddShoppingAgentCore_RegistersMeterFactory_ForShoppingAgentMetrics()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        var configuration = CreateConfiguration();

        // Act
        services.AddShoppingAgentCore(configuration, adapters =>
        {
            adapters.AddScoped<IPreferencesService>(_ => Substitute.For<IPreferencesService>());
            adapters.AddScoped<ISessionService>(_ => Substitute.For<ISessionService>());
        });

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IMeterFactory));
    }
}
