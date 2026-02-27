using BizLogic;
using DataLayer.EF;
using DTO.Ingredient;

namespace ServiceLayer.Concrete;

public class IngredientService(IIngredientAction ingredientAction, EfCoreContext context) : IIngredientService
{
    public ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto)
    {
        var createdIngredientDto = ingredientAction.CreateIngredient(newIngredientDto);
        context.SaveChanges();

        return createdIngredientDto;
    }

    /// <inheritdoc />
    public void DeleteIngredient(DeleteIngredientDto deleteIngredientDto)
    {
        ingredientAction.DeleteIngredient(deleteIngredientDto);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public IEnumerable<ExistingIngredientDto> GetAllIngredients() => ingredientAction.GetAllIngredients();
}
