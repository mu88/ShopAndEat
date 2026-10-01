using DTO.Ingredient;

namespace ServiceLayer;

public interface IIngredientService
{
    Task<ExistingIngredientDto> CreateIngredientAsync(NewIngredientDto newIngredientDto, CancellationToken cancellationToken = default);

    Task DeleteIngredientAsync(DeleteIngredientDto deleteIngredientDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingIngredientDto>> GetAllIngredientsAsync(CancellationToken cancellationToken = default);
}
