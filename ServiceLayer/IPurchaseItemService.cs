using DTO.PurchaseItem;

namespace ServiceLayer;

internal interface IPurchaseItemService
{
    ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto);

    void DeletePurchaseItem(DeletePurchaseItemDto deletePurchaseItemDto);
}
