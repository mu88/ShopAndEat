using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="Store"/>. Use <c>new StoreBuilder().WithDefaults().Build()</c>.</summary>
public class StoreBuilder
{
    private readonly List<Action<Store>> _postBuildActions = [];
    private string _name = string.Empty;
    private List<ShoppingOrder> _compartments = [];

    public StoreBuilder WithDefaults()
    {
        _name = "Coop";
        _compartments = [];
        _postBuildActions.Clear();
        return this;
    }

    /// <summary>Adds a further compartment via the real <see cref="Store.AddCompartment"/> business method, which enforces that no two compartments share the same order.</summary>
    public StoreBuilder WithAdditionalCompartment(ShoppingOrder compartment)
    {
        _postBuildActions.Add(store => store.AddCompartment(compartment));
        return this;
    }

    public Store Build()
    {
        var store = new Store(_name, _compartments);
        foreach (var postBuildAction in _postBuildActions)
        {
            postBuildAction(store);
        }

        return store;
    }
}
