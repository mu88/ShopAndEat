using DTO.Recipe;

namespace ServiceLayer;

public interface IRecipeService
{
    IEnumerable<ExistingRecipeDto> GetAllRecipes();

    void CreateNewRecipe(NewRecipeDto newRecipeDto);

    void DeleteRecipe(DeleteRecipeDto recipeToDelete);
        
    void UpdateRecipe(UpdateRecipeDto existingRecipeDto);
}