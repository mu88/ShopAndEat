using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public class ExistingMealDto
{
    public ExistingMealDto(DateTime day,
                           ExistingMealTypeDto mealType,
                           ExistingRecipeDto recipe,
                           int mealId,
                           bool hasBeenShopped,
                           int numberOfPersons)
    {
        Day = day;
        MealType = mealType;
        Recipe = recipe;
        MealId = mealId;
        HasBeenShopped = hasBeenShopped;
        NumberOfPersons = numberOfPersons;
    }

    public DateTime Day { get; }

    public ExistingMealTypeDto MealType { get; }

    public ExistingRecipeDto Recipe { get; }

    public int MealId { get; }

    public bool HasBeenShopped { get; }
    
    public int NumberOfPersons { get; }
}