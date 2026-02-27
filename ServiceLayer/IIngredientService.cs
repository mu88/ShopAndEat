using DTO.Ingredient;

namespace ServiceLayer;

public interface IIngredientService
{
    ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto);

    void DeleteIngredient(DeleteIngredientDto deleteIngredientDto);

    IEnumerable<ExistingIngredientDto> GetAllIngredients();
}
