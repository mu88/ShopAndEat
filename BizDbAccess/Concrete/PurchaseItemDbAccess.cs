using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class PurchaseItemDbAccess : IPurchaseItemDbAccess
{
    public PurchaseItemDbAccess(EfCoreContext context)
    {
        Context = context;
    }

    private EfCoreContext Context { get; }

    /// <inheritdoc />
    public PurchaseItem AddPurchaseItem(PurchaseItem purchaseItem)
    {
        return Context.PurchaseItems.Add(purchaseItem).Entity;
    }

    /// <inheritdoc />
    public void DeletePurchaseItem(PurchaseItem purchaseItem)
    {
        Context.PurchaseItems.Remove(purchaseItem);
    }

    /// <inheritdoc />
    public PurchaseItem GetPurchaseItem(int purchaseItemId)
    {
        return Context.PurchaseItems.Single(x => x.PurchaseItemId == purchaseItemId);
    }
}