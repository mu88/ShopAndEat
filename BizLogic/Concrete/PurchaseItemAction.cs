using BizDbAccess;
using DTO.PurchaseItem;

namespace BizLogic.Concrete;

public class PurchaseItemAction(IPurchaseItemDbAccess purchaseItemDbAccess) : IPurchaseItemAction
{
    public ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto)
    {
        var newPurchaseItem = newPurchaseItemDto.ToEntity();
        var createdPurchaseItem = purchaseItemDbAccess.AddPurchaseItem(newPurchaseItem);

        return createdPurchaseItem.ToDto();
    }

    /// <inheritdoc />
    public void DeletePurchaseItem(DeletePurchaseItemDto deletePurchaseItemDto)
        => purchaseItemDbAccess.DeletePurchaseItem(purchaseItemDbAccess.GetPurchaseItem(deletePurchaseItemDto.PurchaseItemId));
}
