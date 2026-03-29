using System.Globalization;

namespace DataLayer.EfClasses;

public readonly record struct ShoppingPreferenceId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
