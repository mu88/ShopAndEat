namespace ShoppingAgent.Models;

/// <summary>
/// Represents a product found in an online shop's search results.
/// </summary>
public record ShopProduct
{
    public string Name { get; init; } = string.Empty;

    public string Price { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string ImageUrl { get; init; } = string.Empty;

    public bool IsAvailable { get; init; } = true;
}
