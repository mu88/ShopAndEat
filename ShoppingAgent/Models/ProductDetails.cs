namespace ShoppingAgent.Models;

/// <summary>
/// Detailed information from a product detail page in an online shop.
/// </summary>
public record ProductDetails
{
    public string Name { get; init; } = string.Empty;

    public string Price { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string UnitSize { get; init; } = string.Empty;

    public string Brand { get; init; } = string.Empty;

    public bool IsAvailable { get; init; } = true;

    public string Description { get; init; } = string.Empty;
}
