using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IIngredientDbAccess
{
    Ingredient AddIngredient(Ingredient ingredient);

    void DeleteIngredient(Ingredient ingredient);

    Ingredient GetIngredient(int ingredientId);

    IEnumerable<Ingredient> GetIngredients();
}
