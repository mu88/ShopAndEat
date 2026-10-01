using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="EfUnit"/>. Use <c>new UnitBuilder().WithDefaults().Build()</c>.</summary>
public class UnitBuilder
{
    private string _name = string.Empty;

    public UnitBuilder WithDefaults()
    {
        _name = "Piece";
        return this;
    }

    public EfUnit Build() => new(_name);
}
