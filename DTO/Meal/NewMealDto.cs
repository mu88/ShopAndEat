using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public class NewMealDto(DateTime day, ExistingMealTypeDto mealType, ExistingRecipeDto recipe, int numberOfPersons, int numberOfDays)
{
    public DateTime Day { get; } = day;

    public ExistingMealTypeDto MealType { get; } = mealType;

    public ExistingRecipeDto Recipe { get; } = recipe;

    public int NumberOfPersons { get; set; } = numberOfPersons;

    public int NumberOfDays { get; set; } = numberOfDays;
}
