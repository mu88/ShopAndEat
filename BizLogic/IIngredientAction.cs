using DTO.Ingredient;

namespace BizLogic;

public interface IIngredientAction
{
    ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto);

    void DeleteIngredient(DeleteIngredientDto deleteIngredientDto);

    IEnumerable<ExistingIngredientDto> GetAllIngredients();
}