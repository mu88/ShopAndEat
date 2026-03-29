using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

/// <summary>
/// Shop-agnostic interface for executing browser-based tool calls.
/// Each implementation handles a specific online shop (e.g. Coop, Migros).
/// </summary>
public interface IShopToolExecutor
{
    Task<IReadOnlyList<ShopProduct>> SearchAsync(string searchTerm, CancellationToken ct = default);

    Task<ProductDetails> GetProductDetailsAsync(string productUrl, CancellationToken ct = default);

    Task<string> AddToCartAsync(string productUrl, int quantity, CancellationToken ct = default);

    Task<string> RemoveFromCartAsync(string productName, CancellationToken ct = default);

    Task<string> GetCartContentsAsync(CancellationToken ct = default);

    Task<string> NavigateToCartAsync(CancellationToken ct = default);
}
