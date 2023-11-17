using DTO.Ingredient;

namespace DTO.Recipe;

public class NewRecipeDto
{
    public NewRecipeDto(string name, in int numberOfDays, int numberOfPersons, IEnumerable<NewIngredientDto> ingredients)
    {
        Name = name;
        NumberOfDays = numberOfDays;
        Ingredients = ingredients;
        NumberOfPersons = numberOfPersons;
    }

    public string Name { get; }

    public int NumberOfDays { get; }
    
    public int NumberOfPersons { get; }

    public IEnumerable<NewIngredientDto> Ingredients { get; }
}