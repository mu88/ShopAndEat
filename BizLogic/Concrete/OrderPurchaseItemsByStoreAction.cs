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
                    store.Compartments.Single(compartment => compartment.ArticleGroup == purchaseItem.Article.ArticleGroup)))
            .OrderBy(orderedPurchaseItem => orderedPurchaseItem.Value.Order)
            .ThenBy(orderedPurchaseItem => orderedPurchaseItem.Key.Article.Name)
            .ThenBy(orderedPurchaseItem => orderedPurchaseItem.Key.Unit.Name)
            .Select(orderedPurchaseItem => orderedPurchaseItem.Key);
}
