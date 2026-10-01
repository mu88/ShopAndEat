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
    public async Task DeleteIngredientAsync(DeleteIngredientDto deleteIngredientDto, CancellationToken cancellationToken = default)
    {
        ingredientDbAccess.DeleteIngredient(await ingredientDbAccess.GetIngredientAsync(deleteIngredientDto.IngredientId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingIngredientDto>> GetAllIngredientsAsync(CancellationToken cancellationToken = default)
    {
        var ingredients = await ingredientDbAccess.GetIngredientsAsync(cancellationToken);

        return ingredients.Select(ingredient => ingredient.ToDto()).ToList();
    }
}
