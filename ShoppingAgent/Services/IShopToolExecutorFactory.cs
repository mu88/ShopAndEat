using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

/// <summary>
/// Factory that provides the appropriate <see cref="IShopToolExecutor"/> for a given shop.
/// </summary>
public interface IShopToolExecutorFactory
{
    IShopToolExecutor GetExecutor(string shopKey);

    IReadOnlyList<ShopConfig> AvailableShops { get; }
}
