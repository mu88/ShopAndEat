using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Purchase
{
    private readonly List<PurchaseItem> _purchaseItems;

    public Purchase(DateTime from, DateTime to, IEnumerable<PurchaseItem> purchaseItems)
    {
        _purchaseItems = purchaseItems.ToList();
        From = from;
        To = to;
    }

    [UsedImplicitly]
    private Purchase()
    {
    }

    public virtual IEnumerable<PurchaseItem> PurchaseItems => _purchaseItems;

    public DateTime From
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public DateTime To
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int PurchaseId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}
