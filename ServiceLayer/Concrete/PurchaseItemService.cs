using BizLogic;
using DataLayer.EF;
using DTO.PurchaseItem;

namespace ServiceLayer.Concrete;

public class PurchaseItemService(IPurchaseItemAction purchaseItemAction, EfCoreContext context) : IPurchaseItemService
{
    public ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto)
    {
        var createdPurchaseItemDto = purchaseItemAction.CreatePurchaseItem(newPurchaseItemDto);
        context.SaveChanges();

        return createdPurchaseItemDto;
    }

    /// <inheritdoc />
    public void DeletePurchaseItem(DeletePurchaseItemDto deletePurchaseItemDto)
    {
        purchaseItemAction.DeletePurchaseItem(deletePurchaseItemDto);
        context.SaveChanges();
    }
}