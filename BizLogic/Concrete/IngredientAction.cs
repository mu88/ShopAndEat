using AutoMapper;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.Ingredient;

namespace BizLogic.Concrete;

public class IngredientAction : IIngredientAction
{
    public IngredientAction(IIngredientDbAccess ingredientDbAccess, IMapper mapper)
    {
        IngredientDbAccess = ingredientDbAccess;
        Mapper = mapper;
    }

    private IIngredientDbAccess IngredientDbAccess { get; }

    private IMapper Mapper { get; }

    /// <inheritdoc />
    public ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto)
    {
        var newIngredient = Mapper.Map<Ingredient>(newIngredientDto);
        var createdIngredient = IngredientDbAccess.AddIngredient(newIngredient);

        return Mapper.Map<ExistingIngredientDto>(createdIngredient);
    }

    /// <inheritdoc />
    public void DeleteIngredient(DeleteIngredientDto deleteIngredientDto)
    {
        IngredientDbAccess.DeleteIngredient(IngredientDbAccess.GetIngredient(deleteIngredientDto.IngredientId));
    }

    /// <inheritdoc />
    public IEnumerable<ExistingIngredientDto> GetAllIngredients()
    {
        var ingredients = IngredientDbAccess.GetIngredients();

        return Mapper.Map<IEnumerable<ExistingIngredientDto>>(ingredients);
    }
}