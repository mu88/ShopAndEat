using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Unit : IHasId<UnitId>
{
    public Unit(string name) => Name = name;

    public Unit()
    {
        Name = string.Empty;
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public UnitId UnitId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}
