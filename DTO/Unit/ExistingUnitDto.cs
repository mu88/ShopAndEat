namespace DTO.Unit;

public class ExistingUnitDto(int unitId, string name)
{
    public int UnitId { get; } = unitId;

    public string Name { get; } = name;
}