using DataLayer.EfClasses;
using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public record ExistingMealDto(
    DateTime Day,
    ExistingMealTypeDto MealType,
    ExistingRecipeDto Recipe,
    MealId MealId,
    bool HasBeenShopped,
    int NumberOfPersons);
