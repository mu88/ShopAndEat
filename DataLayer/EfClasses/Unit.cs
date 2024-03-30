using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Unit
{
    public Unit(string name) => Name = name;

    public Unit()
    {
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int UnitId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}