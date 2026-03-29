using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingAgent.Models;
using ShoppingAgent.Options;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Provides shop-specific tool executors. Currently only Coop is supported.
/// New shops can be added by registering additional executors.
/// </summary>
public class ShopToolExecutorFactory(IExtensionBridge bridge, ILoggerFactory loggerFactory, IOptions<ShopOptions> shopOptions) : IShopToolExecutorFactory
{
    private readonly ConcurrentDictionary<string, IShopToolExecutor> _executors = new();

    public IReadOnlyList<ShopConfig> AvailableShops => shopOptions.Value.Shops;

    public IShopToolExecutor GetExecutor(string shopKey) => _executors.GetOrAdd(shopKey, CreateExecutor);

    private IShopToolExecutor CreateExecutor(string shopKey) => shopKey switch
    {
        "coop" => new CoopToolExecutor(bridge, loggerFactory.CreateLogger<CoopToolExecutor>()),
        _ => throw new ArgumentException($"Unknown shop: {shopKey}", nameof(shopKey)),
    };
}
