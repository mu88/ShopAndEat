namespace ShoppingAgent.Services;

public record SessionSummary
{
    public int SessionId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string IngredientList { get; init; } = string.Empty;

    public int ItemCount { get; init; }
}

public record SessionItemDto
{
    public string OriginalIngredient { get; init; } = string.Empty;

    public string SelectedProductName { get; init; } = string.Empty;

    public string SelectedProductUrl { get; init; } = string.Empty;

    public int Quantity { get; init; } = 1;

    public string Price { get; init; } = string.Empty;
}

public record IngredientItem
{
    public string Text { get; init; } = string.Empty;

    public string Article { get; init; } = string.Empty;

    public double Quantity { get; init; }

    public string Unit { get; init; } = string.Empty;
}
