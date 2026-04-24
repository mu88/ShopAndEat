using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace ShoppingAgent;

public static class ShoppingAgentExtensions
{
    /// <summary>
    /// Registers all core ShoppingAgent services and options.
    /// Call <paramref name="registerAdapters"/> to supply concrete implementations
    /// of <see cref="IPreferencesService"/> and <see cref="ISessionService"/>
    /// — these depend on infrastructure that lives outside this project.
    /// </summary>
    public static IServiceCollection AddShoppingAgentCore(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceCollection> registerAdapters)
    {
        services.Configure<LlmClientOptions>(configuration.GetSection(LlmClientOptions.SectionName));
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<ExtensionOptions>(configuration.GetSection(ExtensionOptions.SectionName));
        services.Configure<ShopOptions>(configuration.GetSection(ShopOptions.SectionName));

        services.AddLocalization();
        services.AddMetrics();
        services.AddSingleton<ShoppingAgentMetrics>();
        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient<IMistralChatClientProvider, MistralChatClientProvider>();
        services.AddScoped<IShoppingWorkflowState, ShoppingWorkflowState>();
        services.AddScoped<IExtensionBridge, ExtensionBridge>();
        services.AddScoped<IShopToolExecutorFactory, ShopToolExecutorFactory>();
        services.AddScoped<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddScoped<IToolDefinitionProvider, ToolDefinitionProvider>();
        services.AddScoped<IShoppingListVerifier, ShoppingListVerifier>();
        services.AddScoped<IToolCallDispatcher, ToolCallDispatcher>();
        services.AddScoped<IToolResultRenderer, HtmlToolResultRenderer>();
        services.AddSingleton<IToolResultCompressor, ToolResultCompressor>();
        services.AddScoped<IConversationManager, ConversationManager>();
        services.AddScoped<IShopSessionManager, ShopSessionManager>();
        services.AddScoped<IAgentService, AgentService>();

        registerAdapters(services);

        return services;
    }

    /// <summary>
    /// Adds the ShoppingAgent assembly to the Razor component endpoint so that
    /// <see cref="Pages.Home"/> is discoverable at <c>/shopping</c>.
    /// </summary>
    public static RazorComponentsEndpointConventionBuilder MapShoppingAgent(
        this RazorComponentsEndpointConventionBuilder builder) =>
        builder.AddAdditionalAssemblies(typeof(Pages.Home).Assembly);
}
