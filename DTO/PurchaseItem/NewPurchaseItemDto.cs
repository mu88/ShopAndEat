using DTO.Article;
using DTO.Unit;

namespace DTO.PurchaseItem;

public class NewPurchaseItemDto(ExistingArticleDto article, ExistingUnitDto unit, double quantity)
{
    public ExistingArticleDto Article { get; } = article;

    public ExistingUnitDto Unit { get; } = unit;

    public double Quantity { get; } = quantity;

    /// <inheritdoc />
    public override string ToString() =>
        string.Equals(Unit.Name, "Stück", StringComparison.Ordinal) ? $"{Quantity} {Article.Name}" : $"{Quantity} {Unit.Name} {Article.Name}";
}