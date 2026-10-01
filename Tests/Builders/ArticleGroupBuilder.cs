using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="ArticleGroup"/>. Use <c>new ArticleGroupBuilder().WithDefaults().Build()</c>.</summary>
public class ArticleGroupBuilder
{
    private string _name = string.Empty;

    public ArticleGroupBuilder WithDefaults()
    {
        _name = "Vegetables";
        return this;
    }

    public ArticleGroup Build() => new(_name);
}
