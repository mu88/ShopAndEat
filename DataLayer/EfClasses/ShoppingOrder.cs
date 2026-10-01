using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class ShoppingOrder
{
    // MA0056: assign the backing field directly (not the virtual property) from the constructor.
    private ArticleGroup _articleGroup;

    public ShoppingOrder(ArticleGroup articleGroup, int order)
    {
        _articleGroup = articleGroup;
        Order = order;
    }

    // Stryker disable once all: this null-forgiving assignment only satisfies the compiler for EF Core materialization; removing it leaves the same runtime state.
    public ShoppingOrder()
    {
        _articleGroup = null!;
    }

    public virtual ArticleGroup ArticleGroup
    {
        get => _articleGroup;
        [UsedImplicitly]
        private set => _articleGroup = value;
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