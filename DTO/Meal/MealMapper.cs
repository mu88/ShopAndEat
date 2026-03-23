using DTO.MealType;
using DTO.Recipe;
using EfMeal = DataLayer.EfClasses.Meal;

namespace DTO.Meal;

public static class MealMapper
{
    public static ExistingMealDto ToDto(this EfMeal entity)
        => new(entity.Day, entity.MealType.ToDto(), entity.Recipe.ToDto(), entity.MealId, entity.HasBeenShopped, entity.NumberOfPersons);
}
