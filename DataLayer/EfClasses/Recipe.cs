using JetBrains.Annotations;

namespace DataLayer.EfClasses;

public class Recipe
{
    public Recipe(string name, int numberOfDays, int numberOfPersons, IEnumerable<Ingredient> ingredients)
    {
        Ingredients = ingredients.ToList();
        Name = name;
        NumberOfDays = numberOfDays;
        NumberOfPersons = numberOfPersons;
    }

    public Recipe(int numberOfPersons) => NumberOfPersons = numberOfPersons;

    public virtual IEnumerable<Ingredient> Ingredients { get; set; }

    public string Name { get; set; }

    public int NumberOfDays { get; set; }

    public int NumberOfPersons { get; set; }

    public int RecipeId
    {
        get;
        [UsedImplicitly]
        private set;
    }
}
