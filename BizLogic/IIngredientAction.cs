using DTO.Ingredient;

namespace BizLogic;

public interface IIngredientAction
{
    ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto);

    Task DeleteIngredientAsync(DeleteIngredientDto deleteIngredientDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingIngredientDto>> GetAllIngredientsAsync(CancellationToken cancellationToken = default);
}
