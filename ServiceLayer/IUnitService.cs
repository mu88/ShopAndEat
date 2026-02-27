using DTO.Unit;

namespace ServiceLayer;

public interface IUnitService
{
    ExistingUnitDto CreateUnit(NewUnitDto newArticleGroupDto);

    void DeleteUnit(DeleteUnitDto deleteArticleGroupDto);

    IEnumerable<ExistingUnitDto> GetAllUnits();
}
