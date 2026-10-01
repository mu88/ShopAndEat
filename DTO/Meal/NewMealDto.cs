using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public record NewMealDto(DateTime Day, ExistingMealTypeDto MealType, ExistingRecipeDto Recipe, int NumberOfPersons, int NumberOfDays);
