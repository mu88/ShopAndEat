using DataLayer.EfClasses;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="PurchaseItem"/>. Use <c>new PurchaseItemBuilder().WithDefaults().Build()</c>.</summary>
public class PurchaseItemBuilder
{
    private Article _article = null!;
    private double _quantity;
    private EfUnit _unit = null!;

    public PurchaseItemBuilder WithDefaults()
    {
        _article = new ArticleBuilder().WithDefaults().Build();
        _quantity = 3;
        _unit = new UnitBuilder().WithDefaults().Build();
        return this;
    }

    public PurchaseItem Build() => new(_article, _quantity, _unit);
}
