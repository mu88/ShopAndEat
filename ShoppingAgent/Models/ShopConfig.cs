namespace ShoppingAgent.Models;

/// <summary>
/// Configuration for a supported online shop.
/// </summary>
public record ShopConfig(string Key, string Name, string BaseUrl, string CartUrl);
