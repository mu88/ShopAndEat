using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

/// <summary>
/// Factory that provides the appropriate <see cref="IShopToolExecutor"/> for a given shop.
/// </summary>
public interface IShopToolExecutorFactory
{
    IReadOnlyList<ShopConfig> AvailableShops { get; }

    IShopToolExecutor GetExecutor(string shopKey);
}
