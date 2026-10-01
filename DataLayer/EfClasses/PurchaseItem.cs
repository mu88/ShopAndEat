using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class PurchaseItem
{
    // MA0056: assign the backing fields directly (not the virtual properties) from the constructor.
    private Article _article;
    private Unit _unit;

    public PurchaseItem(Article article, double quantity, Unit unit)
    {
        _article = article;
        Quantity = quantity;
        _unit = unit;
    }

    // Stryker disable once all: these null-forgiving assignments only satisfy the compiler for EF Core's private materialization constructor; removing them leaves the same runtime state.
    [UsedImplicitly]
    private PurchaseItem()
    {
        _article = null!;
        _unit = null!;
    }

    public virtual Article Article
    {
        get => _article;
        [UsedImplicitly]
        private set => _article = value;
    }

    public virtual Unit Unit
    {
        get => _unit;
        [UsedImplicitly]
        private set => _unit = value;
    }

    public double Quantity
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int PurchaseItemId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}