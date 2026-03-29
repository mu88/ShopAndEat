using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IShopSessionManager
{
    string SelectedShopKey { get; }

    ShopConfig SelectedShop { get; }

    IReadOnlyList<ShopConfig> AvailableShops { get; }

    bool IsInitialized { get; }

    void SelectShop(string shopKey = null);

    void Reset();
}
