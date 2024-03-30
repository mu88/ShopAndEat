using DataLayer.EfClasses;

namespace BizLogic.Concrete;

public class OrderPurchaseItemsByStoreAction : IOrderPurchaseItemsByStoreAction
{
    public IEnumerable<PurchaseItem> OrderPurchaseItemsByStore(Store store, IEnumerable<PurchaseItem> purchaseItems) =>
        purchaseItems
            .Select(purchaseItem =>
                        new KeyValuePair<PurchaseItem, ShoppingOrder>(purchaseItem,
                                                                      store.Compartments.Single(x => x.ArticleGroup ==
                                                                                                     purchaseItem
                                                                                                         .Article.ArticleGroup)))
            .OrderBy(x => x.Value.Order)
            .ThenBy(x => x.Key.Article.Name)
            .ThenBy(x => x.Key.Unit.Name)
            .Select(x => x.Key);
}