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
    public async Task DeletePurchaseItemAsync(DeletePurchaseItemDto deletePurchaseItemDto, CancellationToken cancellationToken = default)
        => purchaseItemDbAccess.DeletePurchaseItem(await purchaseItemDbAccess.GetPurchaseItemAsync(deletePurchaseItemDto.PurchaseItemId, cancellationToken));
}
