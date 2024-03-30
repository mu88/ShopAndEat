namespace DTO.MealType;

public class ExistingMealTypeDto(string name, int mealTypeId, int order)
{
    public string Name { get; } = name;

    public int MealTypeId { get; } = mealTypeId;

    public int Order { get; } = order;
}