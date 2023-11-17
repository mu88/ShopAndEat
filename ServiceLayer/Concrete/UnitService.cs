using DataLayer.EfClasses;
using DTO.Unit;

namespace ServiceLayer.Concrete;

public class UnitService : IUnitService
{
    public UnitService(SimpleCrudHelper simpleCrudHelper)
    {
        SimpleCrudHelper = simpleCrudHelper;
    }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    /// <inheritdoc />
    public ExistingUnitDto CreateUnit(NewUnitDto newArticleGroupDto)
    {
        return SimpleCrudHelper.Create<NewUnitDto, Unit, ExistingUnitDto>(newArticleGroupDto);
    }

    /// <inheritdoc />
    public void DeleteUnit(DeleteUnitDto deleteArticleGroupDto)
    {
        SimpleCrudHelper.Delete<Unit>(deleteArticleGroupDto.UnitId);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingUnitDto> GetAllUnits()
    {
        return SimpleCrudHelper.GetAllAsDto<Unit, ExistingUnitDto>();
    }
}