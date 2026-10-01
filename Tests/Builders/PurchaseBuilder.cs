using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Purchase"/>. Use <c>new PurchaseBuilder().WithDefaults().Build()</c>.</summary>
public class PurchaseBuilder
{
    private DateTime _from;
    private DateTime _to;
    private List<PurchaseItem> _purchaseItems = [];

    public PurchaseBuilder WithDefaults()
    {
        _from = new DateTime(2024, 1, 1);
        _to = new DateTime(2024, 1, 7);
        _purchaseItems = [new PurchaseItemBuilder().WithDefaults().Build()];
        return this;
    }

    public Purchase Build() => new(_from, _to, _purchaseItems);
}
