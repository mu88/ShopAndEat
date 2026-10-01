using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="MealType"/>. Use <c>new MealTypeBuilder().WithDefaults().Build()</c>.</summary>
public class MealTypeBuilder
{
    private string _name = string.Empty;
    private int _order;

    public MealTypeBuilder WithDefaults()
    {
        _name = "Dinner";
        _order = 1;
        return this;
    }

    public MealType Build() => new(_name, _order);
}
