using EfMealType = DataLayer.EfClasses.MealType;

namespace DTO.MealType;

public static class MealTypeMapper
{
    public static ExistingMealTypeDto ToDto(this EfMealType entity)
        => new(entity.Name, entity.MealTypeId, entity.Order);

    public static EfMealType ToEntity(this NewMealTypeDto dto)
        => new() { Name = dto.Name };

    public static EfMealType ToEntity(this ExistingMealTypeDto dto)
        => new() { Name = dto.Name, Order = dto.Order };
}
