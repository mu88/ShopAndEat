using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class IngredientDbAccess : IIngredientDbAccess
{
    public IngredientDbAccess(EfCoreContext context)
    {
        Context = context;
    }

    private EfCoreContext Context { get; }

    /// <inheritdoc />
    public Ingredient AddIngredient(Ingredient ingredient)
    {
        return Context.Ingredients.Add(ingredient).Entity;
    }

    /// <inheritdoc />
    public void DeleteIngredient(Ingredient ingredient)
    {
        Context.Ingredients.Remove(ingredient);
    }

    /// <inheritdoc />
    public Ingredient GetIngredient(int ingredientId)
    {
        return Context.Ingredients.Single(x => x.IngredientId == ingredientId);
    }

    /// <inheritdoc />
    public IEnumerable<Ingredient> GetIngredients()
    {
        return Context.Ingredients;
    }
}