using DTO.Recipe;

namespace ServiceLayer;

public interface IRecipeService
{
    Task<IReadOnlyList<ExistingRecipeDto>> GetAllRecipesAsync(CancellationToken cancellationToken = default);

    Task CreateNewRecipeAsync(NewRecipeDto newRecipeDto, CancellationToken cancellationToken = default);

    Task DeleteRecipeAsync(DeleteRecipeDto recipeToDelete, CancellationToken cancellationToken = default);

    Task UpdateRecipeAsync(UpdateRecipeDto existingRecipeDto, CancellationToken cancellationToken = default);
}
