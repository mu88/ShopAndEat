using DTO.MealType;

namespace ServiceLayer;

public interface IMealTypeService
{
    ExistingMealTypeDto CreateMealType(NewMealTypeDto newArticleGroupDto);

    void DeleteMealType(DeleteMealTypeDto deleteArticleGroupDto);

    IEnumerable<ExistingMealTypeDto> GetAllMealTypes();
}
