using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IIngredientDbAccess
{
    Ingredient AddIngredient(Ingredient ingredient);

    void DeleteIngredient(Ingredient ingredient);

    Task<Ingredient> GetIngredientAsync(int ingredientId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken = default);
}