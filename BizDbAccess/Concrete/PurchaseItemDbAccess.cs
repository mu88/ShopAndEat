using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class PurchaseItemDbAccess(EfCoreContext context) : IPurchaseItemDbAccess
{
    public PurchaseItem AddPurchaseItem(PurchaseItem purchaseItem) => context.PurchaseItems.Add(purchaseItem).Entity;

    /// <inheritdoc />
    public void DeletePurchaseItem(PurchaseItem purchaseItem) => context.PurchaseItems.Remove(purchaseItem);

    /// <inheritdoc />
    public Task<PurchaseItem> GetPurchaseItemAsync(int purchaseItemId, CancellationToken cancellationToken = default)
        => context.PurchaseItems.SingleAsync(purchaseItem => purchaseItem.PurchaseItemId == purchaseItemId, cancellationToken);
}