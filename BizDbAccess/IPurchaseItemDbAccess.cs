using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IPurchaseItemDbAccess
{
    PurchaseItem AddPurchaseItem(PurchaseItem purchaseItem);

    void DeletePurchaseItem(PurchaseItem purchaseItem);

    PurchaseItem GetPurchaseItem(int purchaseItemId);
}
