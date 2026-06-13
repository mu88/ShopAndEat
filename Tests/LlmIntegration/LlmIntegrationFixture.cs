using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ShoppingAgent;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;

namespace Tests.LlmIntegration;

/// <summary>
/// Shared fixture for live LLM integration tests.
/// Sets up a real Mistral client with scripted tool executor behavior.
///
/// Requirements:
/// - Set LlmClient__ApiKey environment variable before running tests.
/// - Tests marked with [Explicit] and [Category("LlmIntegration")] to keep them opt-in.
/// </summary>
public sealed class LlmIntegrationFixture : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private ScriptedShopToolExecutor _scriptedToolExecutor;
    private IShoppingWorkflowState _workflowState;

    public IAgentService AgentService { get; }

    public ScriptedShopToolExecutor ToolExecutor => _scriptedToolExecutor;

    public IShoppingWorkflowState WorkflowState => _workflowState;

    public LlmIntegrationFixture()
    {
        var config = BuildConfiguration();
        ValidateApiKey(config);

        var services = new ServiceCollection();

        // Register ShoppingAgent core with real Mistral client
        services.AddShoppingAgentCore(config, registerAdapters: sp =>
        {
            sp.AddScoped<IPreferencesService>(
                _ => Substitute.For<IPreferencesService>());
            sp.AddScoped<ISessionService>(
                _ =>
                {
                    var sessionMock = Substitute.For<ISessionService>();
                    sessionMock.GetUnitsAsync().Returns(new List<string>());
                    return sessionMock;
                });
        });

        // Register scripted tool executor factory that injects deterministic behavior
        services.AddScoped<IShopToolExecutorFactory>(sp =>
        {
            var factoryMock = Substitute.For<IShopToolExecutorFactory>();
            factoryMock.AvailableShops.Returns(new List<ShopConfig>
            {
                new("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
                new("migros", "Migros", "https://www.migros.ch", "https://www.migros.ch/de/warenkorb"),
            });

            _scriptedToolExecutor = new ScriptedShopToolExecutor();
            factoryMock.GetExecutor(Arg.Any<string>()).Returns(_scriptedToolExecutor);

            return factoryMock;
        });

        // Add logging for debugging
        services.AddLogging(builder =>
            builder.AddConsole()
                .SetMinimumLevel(LogLevel.Information));

        _serviceProvider = services.BuildServiceProvider();

        AgentService = _serviceProvider.GetRequiredService<IAgentService>();
        _workflowState = _serviceProvider.GetRequiredService<IShoppingWorkflowState>();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "LlmClient:Endpoint", "https://api.mistral.ai/v1" },
                { "LlmClient:DefaultModel", "mistral-small-2506" },
                { "LlmClient:FallbackModel", "mistral-medium-2508" },
                { "LlmClient:TimeoutSeconds", "60" },
                { "LlmClient:RetryMaxAttempts", "3" },
                { "LlmClient:RetryBaseDelayMs", "1000" },
                { "LlmClient:ApiKey", GetApiKeyFromEnvironment() },
                { "Agent:MaxToolCallIterations", "50" },
                { "Agent:SystemPromptCacheSizeKb", "1024" },
                { "Extension:TimeoutMs", "30000" },
                { "Shop:EnableCoop", "true" },
                { "Shop:EnableMigros", "false" },
            })
            .Build();
    }

    private static string GetApiKeyFromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("LlmClient__ApiKey")
            ?? Environment.GetEnvironmentVariable("LlmClient:ApiKey")
            ?? string.Empty;

        return apiKey;
    }

    private static void ValidateApiKey(IConfiguration config)
    {
        var apiKey = config["LlmClient:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "LlmClient__ApiKey environment variable is not set. " +
                "Live LLM tests require a valid Mistral API key. " +
                "Set it via: $env:LlmClient__ApiKey='your-api-key'");
        }
    }

    public void Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Scripted tool executor that provides deterministic, repeatable behavior
/// for testing LLM reasoning without hitting real shop APIs.
/// </summary>
public sealed class ScriptedShopToolExecutor : IShopToolExecutor
{
    private readonly List<ToolCall> _recordedCalls = new();
    private readonly Dictionary<string, List<ShopProduct>> _searchScripts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProductDetails> _detailsScripts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ToolCall> RecordedCalls => _recordedCalls.AsReadOnly();

    /// <summary>
    /// Pre-script a search response for a given term.
    /// </summary>
    public void ScriptSearch(string term, IReadOnlyList<ShopProduct> products)
    {
        _searchScripts[term] = products.ToList();
    }

    /// <summary>
    /// Pre-script product details for a URL.
    /// </summary>
    public void ScriptDetails(string url, ProductDetails details)
    {
        _detailsScripts[url] = details;
    }

    /// <summary>
    /// Record a workflow-level tool call directly (for testing).
    /// </summary>
    public void RecordWorkflowToolCall(string toolName, IDictionary<string, object> arguments = null)
    {
        _recordedCalls.Add(new ToolCall { ToolName = toolName, Arguments = arguments ?? new Dictionary<string, object>() });
    }

    /// <summary>
    /// Clear all recorded calls and scripts.
    /// </summary>
    public void Reset()
    {
        _recordedCalls.Clear();
        _searchScripts.Clear();
        _detailsScripts.Clear();
    }

    public Task<IReadOnlyList<ShopProduct>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall { ToolName = "search_products", Arguments = new { search_term = searchTerm } });

        if (_searchScripts.TryGetValue(searchTerm, out var products))
        {
            return Task.FromResult((IReadOnlyList<ShopProduct>)products);
        }

        // Return empty results if no script defined
        return Task.FromResult((IReadOnlyList<ShopProduct>)new List<ShopProduct>());
    }

    public Task<ProductDetails> GetProductDetailsAsync(string productUrl, CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall { ToolName = "get_product_details", Arguments = new { product_url = productUrl } });

        if (_detailsScripts.TryGetValue(productUrl, out var details))
        {
            return Task.FromResult(details);
        }

        // Return default details if no script defined
        return Task.FromResult(new ProductDetails
        {
            Name = "Test Product",
            Price = "19.99",
            Description = "A test product",
            Url = productUrl,
        });
    }

    public Task<string> AddToCartAsync(string productUrl, int quantity, CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall
        {
            ToolName = "add_to_cart",
            Arguments = new { product_url = productUrl, quantity }
        });

        return Task.FromResult($"added:{quantity}");
    }

    public Task<string> RemoveFromCartAsync(string productName, string cartEntryUid = null, CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall
        {
            ToolName = "remove_from_cart",
            Arguments = new { product_name = productName, cart_entry_uid = cartEntryUid }
        });

        return Task.FromResult("removed");
    }

    public Task<string> GetCartContentsAsync(CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall { ToolName = "get_cart_contents", Arguments = new object() });

        return Task.FromResult("""
            <div class="cart">
              <div class="item">
                <span class="name">Test Product</span>
                <span class="price">19.99</span>
                <span class="quantity">2</span>
              </div>
            </div>
            """);
    }

    public Task<string> NavigateToCartAsync(CancellationToken ct = default)
    {
        _recordedCalls.Add(new ToolCall { ToolName = "navigate_to_cart", Arguments = new object() });

        return Task.FromResult("navigated to cart");
    }

    public record ToolCall
    {
        public string ToolName { get; set; }
        public object Arguments { get; set; }
    }
}
