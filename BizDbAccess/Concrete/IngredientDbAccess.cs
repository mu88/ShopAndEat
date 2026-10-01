using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class IngredientDbAccess(EfCoreContext context) : IIngredientDbAccess
{
    /// <inheritdoc />
    public Ingredient AddIngredient(Ingredient ingredient) => context.Ingredients.Add(ingredient).Entity;

    /// <inheritdoc />
    public void DeleteIngredient(Ingredient ingredient) => context.Ingredients.Remove(ingredient);

    /// <inheritdoc />
    public Task<Ingredient> GetIngredientAsync(int ingredientId, CancellationToken cancellationToken = default)
        => context.Ingredients.SingleAsync(ingredient => ingredient.IngredientId == ingredientId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken = default)
        => await context.Ingredients.ToListAsync(cancellationToken);
}