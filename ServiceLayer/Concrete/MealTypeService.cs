using DataLayer.EfClasses;
using DTO.MealType;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class MealTypeService(SimpleCrudHelper simpleCrudHelper) : IMealTypeService
{
    public async Task<ExistingMealTypeDto> CreateMealTypeAsync(NewMealTypeDto newArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealTypeService.CreateMealTypeAsync");
        return await simpleCrudHelper.CreateAsync<NewMealTypeDto, MealType, ExistingMealTypeDto>(newArticleGroupDto, dto => dto.ToEntity(), entity => entity.ToDto(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteMealTypeAsync(DeleteMealTypeDto deleteArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealTypeService.DeleteMealTypeAsync");
        await simpleCrudHelper.DeleteAsync<MealType>(deleteArticleGroupDto.MealTypeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingMealTypeDto>> GetAllMealTypesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealTypeService.GetAllMealTypesAsync");
        return await simpleCrudHelper.GetAllAsDtoAsync<MealType, ExistingMealTypeDto>(entity => entity.ToDto(), cancellationToken);
    }
}
