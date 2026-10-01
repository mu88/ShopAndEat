using DataLayer.EfClasses;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Ingredient"/>. Use <c>new IngredientBuilder().WithDefaults().Build()</c>.</summary>
public class IngredientBuilder
{
    private Article _article = null!;
    private double _quantity;
    private EfUnit _unit = null!;

    public IngredientBuilder WithDefaults()
    {
        _article = new ArticleBuilder().WithDefaults().Build();
        _quantity = 2;
        _unit = new UnitBuilder().WithDefaults().Build();
        return this;
    }

    public Ingredient Build() => new(_article, _quantity, _unit);
}
