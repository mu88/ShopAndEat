namespace DTO.PurchaseItem;

public class DeletePurchaseItemDto(int purchaseItemId)
{
    public int PurchaseItemId { get; } = purchaseItemId;
}
