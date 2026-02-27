using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Store
{
    private readonly List<ShoppingOrder> _compartments;

    public Store(string name, IEnumerable<ShoppingOrder> compartments)
    {
        _compartments = compartments.ToList();
        Name = name;
    }

    public Store()
    {
    }

    public virtual IEnumerable<ShoppingOrder> Compartments => _compartments;

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int StoreId
    {
        get;
        [UsedImplicitly]
        private set;
    }

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me here, I'm happy")]
    public void AddCompartment(ShoppingOrder compartment)
    {
        if (_compartments.Exists(x => x.Order == compartment.Order))
        {
            throw new InvalidOperationException($"There is already a compartment with order '{compartment.Order}'");
        }

        _compartments.Add(compartment);
    }

    public void DeleteCompartment(ShoppingOrder compartment) => _compartments.Remove(compartment);
}
