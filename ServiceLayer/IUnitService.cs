using DTO.Unit;

namespace ServiceLayer;

public interface IUnitService
{
    Task<ExistingUnitDto> CreateUnitAsync(NewUnitDto newArticleGroupDto, CancellationToken cancellationToken = default);

    Task DeleteUnitAsync(DeleteUnitDto deleteArticleGroupDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingUnitDto>> GetAllUnitsAsync(CancellationToken cancellationToken = default);
}
