using Microsoft.Extensions.Logging;
using ShoppingAgent.Logging;
using ShoppingAgent.Models;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Manages shop selection, providing the current shop configuration and tool executor resolution.
/// </summary>
public class ShopSessionManager(
    IShopToolExecutorFactory shopToolExecutorFactory,
    ILogger<ShopSessionManager> logger) : IShopSessionManager
{
    public string SelectedShopKey { get; private set; }

    public ShopConfig SelectedShop { get; private set; }

    public IReadOnlyList<ShopConfig> AvailableShops => shopToolExecutorFactory.AvailableShops;

    public bool IsInitialized { get; private set; }

    public void SelectShop(string shopKey = null)
    {
        SelectedShopKey = shopKey ?? shopToolExecutorFactory.AvailableShops[0].Key;
        SelectedShop = shopToolExecutorFactory.AvailableShops.First(shop => shop.Key == SelectedShopKey);
        IsInitialized = true;
        AgentLogMessages.AgentInitialized(logger, SelectedShopKey);
    }

    public void Reset()
    {
        IsInitialized = false;
    }
}
