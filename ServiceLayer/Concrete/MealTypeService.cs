using DataLayer.EfClasses;
using DTO.MealType;

namespace ServiceLayer.Concrete;

public class MealTypeService : IMealTypeService
{
    public MealTypeService(SimpleCrudHelper simpleCrudHelper)
    {
        SimpleCrudHelper = simpleCrudHelper;
    }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    /// <inheritdoc />
    public ExistingMealTypeDto CreateMealType(NewMealTypeDto newArticleGroupDto)
    {
        return SimpleCrudHelper.Create<NewMealTypeDto, MealType, ExistingMealTypeDto>(newArticleGroupDto);
    }

    /// <inheritdoc />
    public void DeleteMealType(DeleteMealTypeDto deleteArticleGroupDto)
    {
        SimpleCrudHelper.Delete<MealType>(deleteArticleGroupDto.MealTypeId);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealTypeDto> GetAllMealTypes()
    {
        return SimpleCrudHelper.GetAllAsDto<MealType, ExistingMealTypeDto>();
    }
}