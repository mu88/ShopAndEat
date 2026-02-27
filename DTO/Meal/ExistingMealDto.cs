using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public class ExistingMealDto(
    DateTime day,
    ExistingMealTypeDto mealType,
    ExistingRecipeDto recipe,
    int mealId,
    bool hasBeenShopped,
    int numberOfPersons)
{
    public DateTime Day { get; } = day;

    public ExistingMealTypeDto MealType { get; } = mealType;

    public ExistingRecipeDto Recipe { get; } = recipe;

    public int MealId { get; } = mealId;

    public bool HasBeenShopped { get; } = hasBeenShopped;

    public int NumberOfPersons { get; } = numberOfPersons;
}
