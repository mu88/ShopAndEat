#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

namespace DTO.IngredientList;

public record IngredientListResponse
{
    public IEnumerable<IngredientItem> Items { get; init; } = [];
}

public record IngredientItem
{
    public string Text { get; init; } = string.Empty;

    public string Article { get; init; } = string.Empty;

    public double Quantity { get; init; }

    public string Unit { get; init; } = string.Empty;
}
