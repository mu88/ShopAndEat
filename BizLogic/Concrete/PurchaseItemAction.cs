using AutoMapper;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.PurchaseItem;

namespace BizLogic.Concrete;

public class PurchaseItemAction(IPurchaseItemDbAccess purchaseItemDbAccess, IMapper mapper) : IPurchaseItemAction
{
    public ExistingPurchaseItemDto CreatePurchaseItem(NewPurchaseItemDto newPurchaseItemDto)
    {
        var newPurchaseItem = mapper.Map<PurchaseItem>(newPurchaseItemDto);
        var createdPurchaseItem = purchaseItemDbAccess.AddPurchaseItem(newPurchaseItem);

        return mapper.Map<ExistingPurchaseItemDto>(createdPurchaseItem);
    }

    /// <inheritdoc />
    public void DeletePurchaseItem(DeletePurchaseItemDto deletePurchaseItemDto)
    {
        purchaseItemDbAccess.DeletePurchaseItem(purchaseItemDbAccess.GetPurchaseItem(deletePurchaseItemDto.PurchaseItemId));
    }
}