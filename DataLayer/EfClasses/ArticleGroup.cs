using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class ArticleGroup : IHasId<ArticleGroupId>
{
    public ArticleGroup(string name) => Name = name;

    public ArticleGroup()
    {
        Name = string.Empty;
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public ArticleGroupId ArticleGroupId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}
