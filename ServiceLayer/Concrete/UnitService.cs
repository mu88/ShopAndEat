using DataLayer.EfClasses;
using DTO.Unit;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class UnitService(SimpleCrudHelper simpleCrudHelper) : IUnitService
{
    public async Task<ExistingUnitDto> CreateUnitAsync(NewUnitDto newArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("UnitService.CreateUnitAsync");
        return await simpleCrudHelper.CreateAsync<NewUnitDto, Unit, ExistingUnitDto>(newArticleGroupDto, dto => dto.ToEntity(), entity => entity.ToDto(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteUnitAsync(DeleteUnitDto deleteArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("UnitService.DeleteUnitAsync");
        await simpleCrudHelper.DeleteAsync<Unit>(deleteArticleGroupDto.UnitId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingUnitDto>> GetAllUnitsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("UnitService.GetAllUnitsAsync");
        return await simpleCrudHelper.GetAllAsDtoAsync<Unit, ExistingUnitDto>(entity => entity.ToDto(), cancellationToken);
    }
}
