using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Models;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Typed wrapper around <see cref="ExtensionBridge"/> for Coop-specific tool calls.
/// Each method corresponds to a tool the LLM can invoke.
/// </summary>
public class CoopToolExecutor : IShopToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string ShopKey = "coop";
    private readonly IExtensionBridge _bridge;
    private readonly ILogger<CoopToolExecutor> _logger;

    public CoopToolExecutor(IExtensionBridge bridge, ILogger<CoopToolExecutor> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    /// <summary>
    /// Searches coop.ch for products matching the given term.
    /// </summary>
    public async Task<IReadOnlyList<ShopProduct>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        ServiceLogMessages.CoopSearching(_logger, searchTerm);
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.Coop.SearchProducts");
        activity?.SetTag("coop.search_term", searchTerm);
        var result = await _bridge.ExecuteToolAsync("search", new Dictionary<string, object> { ["term"] = searchTerm }, ShopKey, ct);

        if (!result.Success)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Error);
            return [];
        }

        var products = JsonSerializer.Deserialize<List<ShopProduct>>(result.Data, JsonOptions) ?? new List<ShopProduct>();
        activity?.SetTag("coop.result_count", products.Count);
        ServiceLogMessages.CoopSearchComplete(_logger, searchTerm, products.Count);
        return products;
    }

    /// <summary>
    /// Navigates to a product detail page and scrapes detailed information.
    /// </summary>
    public async Task<ProductDetails> GetProductDetailsAsync(string productUrl, CancellationToken ct = default)
    {
        ServiceLogMessages.CoopGettingProductDetails(_logger, productUrl);
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.Coop.GetProductDetails");
        activity?.SetTag("coop.product_url", productUrl);
        var result = await _bridge.ExecuteToolAsync("getProductDetails", new Dictionary<string, object> { ["url"] = productUrl }, ShopKey, ct);

        if (!result.Success)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Error);
            return new ProductDetails { Name = "Error", Description = result.Error };
        }

        return JsonSerializer.Deserialize<ProductDetails>(result.Data, JsonOptions) ?? new ProductDetails();
    }

    /// <summary>
    /// Adds a product to the Coop shopping cart.
    /// Returns a detailed result string with added/requested quantities.
    /// </summary>
    public async Task<string> AddToCartAsync(string productUrl, int quantity, CancellationToken ct = default)
    {
        ServiceLogMessages.CoopAddingToCart(_logger, productUrl, quantity);
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.Coop.AddToCart");
        activity?.SetTag("coop.product_url", productUrl);
        activity?.SetTag("coop.quantity", quantity);
        var result = await _bridge.ExecuteToolAsync("addToCart",
            new Dictionary<string, object> { ["url"] = productUrl, ["quantity"] = quantity }, ShopKey, ct);

        if (!result.Success)
            activity?.SetStatus(ActivityStatusCode.Error, result.Error);

        return result.Success ? result.Data : $"ERROR: {result.Error}";
    }

    /// <summary>
    /// Removes a product from the Coop shopping cart by name.
    /// </summary>
    public async Task<string> RemoveFromCartAsync(string productName, CancellationToken ct = default)
    {
        var result = await _bridge.ExecuteToolAsync("removeFromCart",
            new Dictionary<string, object> { ["productName"] = productName }, ShopKey, ct);

        return result.Success ? result.Data : $"ERROR: {result.Error}";
    }

    /// <summary>
    /// Returns the current contents of the Coop shopping cart.
    /// </summary>
    public async Task<string> GetCartContentsAsync(CancellationToken ct = default)
    {
        var result = await _bridge.ExecuteToolAsync("getCartContents", new Dictionary<string, object>(), ShopKey, ct);
        return result.Success ? result.Data : $"ERROR: {result.Error}";
    }

    /// <summary>
    /// Navigates to the cart page.
    /// </summary>
    public async Task<string> NavigateToCartAsync(CancellationToken ct = default)
    {
        var result = await _bridge.ExecuteToolAsync("navigateToCart", new Dictionary<string, object>(), ShopKey, ct);
        return result.Success ? result.Data : $"ERROR: {result.Error}";
    }
}
