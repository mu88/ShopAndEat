using BizDbAccess;
using DTO.Ingredient;

namespace BizLogic.Concrete;

public class IngredientAction(IIngredientDbAccess ingredientDbAccess) : IIngredientAction
{
    public ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto)
    {
        var newIngredient = newIngredientDto.ToEntity();
        var createdIngredient = ingredientDbAccess.AddIngredient(newIngredient);

        return createdIngredient.ToDto();
    }

    /// <inheritdoc />
    public void DeleteIngredient(DeleteIngredientDto deleteIngredientDto)
    {
        ingredientDbAccess.DeleteIngredient(ingredientDbAccess.GetIngredient(deleteIngredientDto.IngredientId));
    }

    /// <inheritdoc />
    public IEnumerable<ExistingIngredientDto> GetAllIngredients()
    {
        var ingredients = ingredientDbAccess.GetIngredients();

        return ingredients.Select(i => i.ToDto());
    }
}
