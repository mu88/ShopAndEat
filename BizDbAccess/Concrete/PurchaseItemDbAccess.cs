using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class PurchaseItemDbAccess(EfCoreContext context) : IPurchaseItemDbAccess
{
    public PurchaseItem AddPurchaseItem(PurchaseItem purchaseItem) => context.PurchaseItems.Add(purchaseItem).Entity;

    /// <inheritdoc />
    public void DeletePurchaseItem(PurchaseItem purchaseItem) => context.PurchaseItems.Remove(purchaseItem);

    /// <inheritdoc />
    public PurchaseItem GetPurchaseItem(int purchaseItemId) => context.PurchaseItems.Single(x => x.PurchaseItemId == purchaseItemId);
}
