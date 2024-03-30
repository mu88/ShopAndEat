using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class ArticleGroup
{
    public ArticleGroup(string name) => Name = name;

    public ArticleGroup()
    {
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int ArticleGroupId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}