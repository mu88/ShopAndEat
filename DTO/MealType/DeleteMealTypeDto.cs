namespace DTO.MealType;

public class DeleteMealTypeDto(int mealTypeId)
{
    public int MealTypeId { get; } = mealTypeId;
}