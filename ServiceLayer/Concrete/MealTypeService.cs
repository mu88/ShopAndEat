using DataLayer.EfClasses;
using DTO.MealType;

namespace ServiceLayer.Concrete;

public class MealTypeService(SimpleCrudHelper simpleCrudHelper) : IMealTypeService
{
    public ExistingMealTypeDto CreateMealType(NewMealTypeDto newArticleGroupDto) => simpleCrudHelper.Create<NewMealTypeDto, MealType, ExistingMealTypeDto>(newArticleGroupDto, dto => dto.ToEntity(), entity => entity.ToDto());

    /// <inheritdoc />
    public void DeleteMealType(DeleteMealTypeDto deleteArticleGroupDto) => simpleCrudHelper.Delete<MealType>(deleteArticleGroupDto.MealTypeId);

    /// <inheritdoc />
    public IEnumerable<ExistingMealTypeDto> GetAllMealTypes() => simpleCrudHelper.GetAllAsDto<MealType, ExistingMealTypeDto>(entity => entity.ToDto());
}
