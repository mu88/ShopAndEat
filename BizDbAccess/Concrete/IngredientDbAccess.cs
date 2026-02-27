using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class IngredientDbAccess(EfCoreContext context) : IIngredientDbAccess
{
    /// <inheritdoc />
    public Ingredient AddIngredient(Ingredient ingredient) => context.Ingredients.Add(ingredient).Entity;

    /// <inheritdoc />
    public void DeleteIngredient(Ingredient ingredient) => context.Ingredients.Remove(ingredient);

    /// <inheritdoc />
    public Ingredient GetIngredient(int ingredientId) => context.Ingredients.Single(x => x.IngredientId == ingredientId);

    /// <inheritdoc />
    public IEnumerable<Ingredient> GetIngredients() => context.Ingredients;
}
