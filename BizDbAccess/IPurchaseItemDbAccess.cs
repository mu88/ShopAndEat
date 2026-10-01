using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IPurchaseItemDbAccess
{
    PurchaseItem AddPurchaseItem(PurchaseItem purchaseItem);

    void DeletePurchaseItem(PurchaseItem purchaseItem);

    Task<PurchaseItem> GetPurchaseItemAsync(int purchaseItemId, CancellationToken cancellationToken = default);
}