using DTO.Ingredient;

namespace DTO.Recipe;

public class NewRecipeDto(string name, in int numberOfDays, int numberOfPersons, IEnumerable<NewIngredientDto> ingredients)
{
    public string Name { get; } = name;

    public int NumberOfDays { get; } = numberOfDays;

    public int NumberOfPersons { get; } = numberOfPersons;

    public IEnumerable<NewIngredientDto> Ingredients { get; } = ingredients;
}
