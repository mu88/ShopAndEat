using DTO.Article;
using DTO.Unit;

namespace DTO.PurchaseItem;

public record ExistingPurchaseItemDto(
    ExistingArticleDto Article,
    ExistingUnitDto Unit,
    uint Quantity,
    int PurchaseItemId);
