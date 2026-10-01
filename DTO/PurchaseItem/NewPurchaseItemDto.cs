using DTO.Article;
using DTO.Unit;

namespace DTO.PurchaseItem;

public record NewPurchaseItemDto(
    ExistingArticleDto Article,
    ExistingUnitDto Unit,
    double Quantity)
{
    /// <inheritdoc />
    public override string ToString() => string.Equals(Unit.Name, "piece", StringComparison.Ordinal) ? $"{Quantity} {Article.Name}" : $"{Quantity} {Unit.Name} {Article.Name}";
}
