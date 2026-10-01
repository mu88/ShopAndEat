using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="ShoppingOrder"/>. Use <c>new ShoppingOrderBuilder().WithDefaults().Build()</c>.</summary>
public class ShoppingOrderBuilder
{
    private ArticleGroup _articleGroup = null!;
    private int _order;

    public ShoppingOrderBuilder WithDefaults()
    {
        _articleGroup = new ArticleGroupBuilder().WithDefaults().Build();
        _order = 1;
        return this;
    }

    public ShoppingOrder Build() => new(_articleGroup, _order);
}
