using DTO.MealType;

namespace ServiceLayer;

public interface IMealTypeService
{
    Task<ExistingMealTypeDto> CreateMealTypeAsync(NewMealTypeDto newArticleGroupDto, CancellationToken cancellationToken = default);

    Task DeleteMealTypeAsync(DeleteMealTypeDto deleteArticleGroupDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingMealTypeDto>> GetAllMealTypesAsync(CancellationToken cancellationToken = default);
}
