using BizLogic;
using DataLayer.EF;
using DTO.Ingredient;

namespace ServiceLayer.Concrete;

public class IngredientService : IIngredientService
{
    public IngredientService(IIngredientAction ingredientAction, EfCoreContext context)
    {
        IngredientAction = ingredientAction;
        Context = context;
    }

    private IIngredientAction IngredientAction { get; }

    private EfCoreContext Context { get; }

    /// <inheritdoc />
    public ExistingIngredientDto CreateIngredient(NewIngredientDto newIngredientDto)
    {
        var createdIngredientDto = IngredientAction.CreateIngredient(newIngredientDto);
        Context.SaveChanges();

        return createdIngredientDto;
    }

    /// <inheritdoc />
    public void DeleteIngredient(DeleteIngredientDto deleteIngredientDto)
    {
        IngredientAction.DeleteIngredient(deleteIngredientDto);
        Context.SaveChanges();
    }

    /// <inheritdoc />
    public IEnumerable<ExistingIngredientDto> GetAllIngredients()
    {
        return IngredientAction.GetAllIngredients();
    }
}