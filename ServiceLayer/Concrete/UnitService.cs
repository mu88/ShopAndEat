using DataLayer.EfClasses;
using DTO.Unit;

namespace ServiceLayer.Concrete;

public class UnitService(SimpleCrudHelper simpleCrudHelper) : IUnitService
{
    public ExistingUnitDto CreateUnit(NewUnitDto newArticleGroupDto)
    {
        return simpleCrudHelper.Create<NewUnitDto, Unit, ExistingUnitDto>(newArticleGroupDto);
    }

    /// <inheritdoc />
    public void DeleteUnit(DeleteUnitDto deleteArticleGroupDto)
    {
        simpleCrudHelper.Delete<Unit>(deleteArticleGroupDto.UnitId);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingUnitDto> GetAllUnits()
    {
        return simpleCrudHelper.GetAllAsDto<Unit, ExistingUnitDto>();
    }
}