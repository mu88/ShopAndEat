using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Recipe : IHasId<RecipeId>
{
    // MA0056: assign the backing field directly (not the virtual property), including from Update(),
    // since Update() is itself called from the constructor.
    private IEnumerable<Ingredient> _ingredients;

    public Recipe(string name, int numberOfDays, int numberOfPersons, IEnumerable<Ingredient> ingredients)
        : this()
    {
        Update(name, numberOfDays, numberOfPersons, ingredients);
    }

    public Recipe()
    {
        // IDE0028: intentionally NOT simplified to a collection expression ('[]') — that would
        // produce a fixed-size Array instead of a List<Ingredient>, which breaks EF Core's
        // materialization via this private backing field ("Collection was of a fixed size").
#pragma warning disable IDE0028
        _ingredients = new List<Ingredient>();
#pragma warning restore IDE0028
        Name = string.Empty;
    }

    public virtual IEnumerable<Ingredient> Ingredients
    {
        get => _ingredients;
        [UsedImplicitly]
        private set => _ingredients = value;
    }

    public string Name
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int NumberOfDays
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public int NumberOfPersons
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public RecipeId RecipeId
    {
        get;
        [UsedImplicitly]
        private set;
    }

    public void Update(string name, int numberOfDays, int numberOfPersons, IEnumerable<Ingredient> ingredients)
    {
        _ingredients = ingredients.ToList();
        Name = name;
        NumberOfDays = numberOfDays;
        NumberOfPersons = numberOfPersons;
    }
}
