using DataLayer.EfClasses;

namespace BizLogic;

public interface IOrderPurchaseItemsByStoreAction
{
    IEnumerable<PurchaseItem> OrderPurchaseItemsByStore(Store store, IEnumerable<PurchaseItem> purchaseItems);
}