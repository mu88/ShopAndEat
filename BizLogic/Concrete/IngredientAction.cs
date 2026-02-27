using AutoMapper;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.Ingredient;

namespace BizLogic.Concrete;

public class IngredientAction(IIngredientDbAccess ingredientDbAccess, IMapper mapper) : IIngredientAction
{
    public ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto)
    {
        var newIngredient = mapper.Map<Ingredient>(newIngredientDto);
        var createdIngredient = ingredientDbAccess.AddIngredient(newIngredient);

        return mapper.Map<ExistingIngredientDto>(createdIngredient);
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

        return mapper.Map<IEnumerable<ExistingIngredientDto>>(ingredients);
    }
}
