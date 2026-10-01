using BizLogic;
using DataLayer.EF;
using DTO.PurchaseItem;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class PurchaseItemService(IPurchaseItemAction purchaseItemAction, EfCoreContext context) : IPurchaseItemService
{
    public async Task<ExistingPurchaseItemDto> CreatePurchaseItemAsync(NewPurchaseItemDto newPurchaseItemDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("PurchaseItemService.CreatePurchaseItemAsync");
        var createdPurchaseItemDto = purchaseItemAction.CreatePurchaseItem(newPurchaseItemDto);
        await context.SaveChangesAsync(cancellationToken);

        return createdPurchaseItemDto;
    }

    /// <inheritdoc />
    public async Task DeletePurchaseItemAsync(DeletePurchaseItemDto deletePurchaseItemDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("PurchaseItemService.DeletePurchaseItemAsync");
        await purchaseItemAction.DeletePurchaseItemAsync(deletePurchaseItemDto, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
