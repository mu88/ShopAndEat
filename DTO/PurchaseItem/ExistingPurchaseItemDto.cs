using DTO.Article;
using DTO.Unit;

namespace DTO.PurchaseItem;

public class ExistingPurchaseItemDto(
    ExistingArticleDto article,
    ExistingUnitDto unit,
    uint quantity,
    int purchaseItemId)
{
    public ExistingArticleDto Article { get; } = article;

    public ExistingUnitDto Unit { get; } = unit;

    public uint Quantity { get; } = quantity;

    public int PurchaseItemId { get; } = purchaseItemId;
}
