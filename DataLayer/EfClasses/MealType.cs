using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class MealType : IHasId<int>
{
    public MealType(string name, int order)
    {
        Name = name;
        Order = order;
    }

    public MealType()
    {
        Name = string.Empty;
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int Order
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int MealTypeId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}