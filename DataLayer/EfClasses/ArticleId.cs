using System.Globalization;

namespace DataLayer.EfClasses;

public readonly record struct ArticleId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}