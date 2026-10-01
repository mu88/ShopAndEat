using EfMealType = DataLayer.EfClasses.MealType;

namespace DTO.MealType;

public static class MealTypeMapper
{
    public static ExistingMealTypeDto ToDto(this EfMealType entity)
        => new(entity.Name, entity.MealTypeId, entity.Order);

    public static EfMealType ToEntity(this NewMealTypeDto dto)
        => new(dto.Name, 0);

    public static EfMealType ToEntity(this ExistingMealTypeDto dto)
        => new(dto.Name, dto.Order);
}
