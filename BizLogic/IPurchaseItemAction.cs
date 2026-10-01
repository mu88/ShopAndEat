using DTO.PurchaseItem;

namespace BizLogic;

public interface IPurchaseItemAction
{
    ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto);

    Task DeletePurchaseItemAsync(DeletePurchaseItemDto deletePurchaseItemDto, CancellationToken cancellationToken = default);
}
