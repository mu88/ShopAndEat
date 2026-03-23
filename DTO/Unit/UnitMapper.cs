using EfUnit = DataLayer.EfClasses.Unit;

namespace DTO.Unit;

public static class UnitMapper
{
    public static ExistingUnitDto ToDto(this EfUnit entity)
        => new(entity.UnitId, entity.Name);

    public static EfUnit ToEntity(this NewUnitDto dto)
        => new(dto.Name);

    public static EfUnit ToEntity(this ExistingUnitDto dto)
        => new(dto.Name);
}
