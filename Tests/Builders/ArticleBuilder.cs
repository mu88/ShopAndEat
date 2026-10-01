using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Article"/>. Use <c>new ArticleBuilder().WithDefaults().Build()</c>.</summary>
public class ArticleBuilder
{
    private string _name = string.Empty;
    private ArticleGroup _articleGroup = null!;
    private bool _isInventory;

    public ArticleBuilder WithDefaults()
    {
        _name = "Tomato";
        _articleGroup = new ArticleGroupBuilder().WithDefaults().Build();
        _isInventory = false;
        return this;
    }

    public Article Build() => new(_name, _articleGroup, _isInventory);
}
