using System;
using DTO.MealType;
using DTO.Recipe;

namespace DTO.Meal;

public class NewMealDto
{
    public NewMealDto(DateTime day, ExistingMealTypeDto mealType, ExistingRecipeDto recipe, int numberOfPersons, int numberOfDays)
    {
        Day = day;
        MealType = mealType;
        Recipe = recipe;
        NumberOfPersons = numberOfPersons;
        NumberOfDays = numberOfDays;
    }

    public DateTime Day { get; }

    public ExistingMealTypeDto MealType { get; }

    public ExistingRecipeDto Recipe { get; }
    
    public int NumberOfPersons { get; set; }
    public int NumberOfDays { get; set; }
}