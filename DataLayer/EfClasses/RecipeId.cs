using System.Globalization;

namespace DataLayer.EfClasses;

public readonly record struct RecipeId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
