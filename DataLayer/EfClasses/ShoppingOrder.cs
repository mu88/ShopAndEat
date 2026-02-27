using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class ShoppingOrder
{
    public ShoppingOrder(ArticleGroup articleGroup, int order)
    {
        ArticleGroup = articleGroup;
        Order = order;
    }

    public ShoppingOrder()
    {
    }

    public virtual ArticleGroup ArticleGroup
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int Order
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int ShoppingOrderId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}
