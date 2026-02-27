using System.Diagnostics.CodeAnalysis;
using DataLayer.EfClasses;

namespace BizLogic.Concrete;

public class OrderPurchaseItemsByStoreAction : IOrderPurchaseItemsByStoreAction
{
    [SuppressMessage("Usage", "MA0002:IEqualityComparer<string> or IComparer<string> is missing", Justification = "Okay for me here, I'm happy")]
    public IEnumerable<PurchaseItem> OrderPurchaseItemsByStore(Store store, IEnumerable<PurchaseItem> purchaseItems)
        => purchaseItems
            .Select(purchaseItem =>
                new KeyValuePair<PurchaseItem, ShoppingOrder>(purchaseItem,
                    store.Compartments.Single(x => x.ArticleGroup == purchaseItem.Article.ArticleGroup)))
            .OrderBy(x => x.Value.Order)
            .ThenBy(x => x.Key.Article.Name)
            .ThenBy(x => x.Key.Unit.Name)
            .Select(x => x.Key);
}
