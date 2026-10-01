using BizLogic;
using DataLayer.EF;
using DTO.Ingredient;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class IngredientService(IIngredientAction ingredientAction, EfCoreContext context) : IIngredientService
{
    public async Task<ExistingIngredientDto> CreateIngredientAsync(NewIngredientDto newIngredientDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("IngredientService.CreateIngredientAsync");
        var createdIngredientDto = ingredientAction.CreateIngredient(newIngredientDto);
        await context.SaveChangesAsync(cancellationToken);

        return createdIngredientDto;
    }

    /// <inheritdoc />
    public async Task DeleteIngredientAsync(DeleteIngredientDto deleteIngredientDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("IngredientService.DeleteIngredientAsync");
        await ingredientAction.DeleteIngredientAsync(deleteIngredientDto, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingIngredientDto>> GetAllIngredientsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("IngredientService.GetAllIngredientsAsync");
        return await ingredientAction.GetAllIngredientsAsync(cancellationToken);
    }
}
