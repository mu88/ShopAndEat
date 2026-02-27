using DTO.PurchaseItem;

namespace BizLogic;

public interface IPurchaseItemAction
{
    ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto);

    void DeletePurchaseItem(DeletePurchaseItemDto deletePurchaseItemDto);
}
