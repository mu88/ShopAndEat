using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Article : IHasId<ArticleId>
{
    // MA0056: assign the backing field directly (not the virtual property), including from Update(),
    // since Update() is itself called from the constructor.
    private ArticleGroup _articleGroup;

    public Article(string name, ArticleGroup articleGroup, bool isInventory = false)
        : this()
    {
        Update(name, articleGroup, isInventory);
    }

    public Article()
    {
        Name = string.Empty;
        _articleGroup = null!;
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public virtual ArticleGroup ArticleGroup
    {
        get => _articleGroup;
        [UsedImplicitly]
        private set => _articleGroup = value;
    }

    public bool IsInventory { get; private set; }

    public ArticleId ArticleId
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public void Update(string name, ArticleGroup articleGroup, bool isInventory)
    {
        Name = name;
        _articleGroup = articleGroup;
        IsInventory = isInventory;
    }
}