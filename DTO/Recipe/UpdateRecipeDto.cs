using DTO.Ingredient;

namespace DTO.Recipe;

public class UpdateRecipeDto(
    string name,
    int numberOfDays,
    int numberOfPersons,
    IEnumerable<NewIngredientDto> ingredients,
    int recipeId)
{
    public string Name { get; } = name;

    public int NumberOfDays { get; } = numberOfDays;

    public int NumberOfPersons { get;  } = numberOfPersons;

    public IEnumerable<NewIngredientDto> Ingredients { get; } = ingredients;

    public int RecipeId { get; } = recipeId;
}