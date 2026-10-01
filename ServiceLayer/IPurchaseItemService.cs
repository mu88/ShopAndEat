using DTO.PurchaseItem;

namespace ServiceLayer;

internal interface IPurchaseItemService
{
    Task<ExistingPurchaseItemDto> CreatePurchaseItemAsync(NewPurchaseItemDto newPurchaseItemDto, CancellationToken cancellationToken = default);

    Task DeletePurchaseItemAsync(DeletePurchaseItemDto deletePurchaseItemDto, CancellationToken cancellationToken = default);
}
