using DTO.Ingredient;

namespace DTO.Recipe;

public class ExistingRecipeDto(
    string name,
    int numberOfDays,
    int numberOfPersons,
    IEnumerable<ExistingIngredientDto> ingredients,
    int recipeId)
{
    public string Name { get; } = name;

    public int NumberOfDays { get; } = numberOfDays;

    public int NumberOfPersons { get;  } = numberOfPersons;

    public IEnumerable<ExistingIngredientDto> Ingredients { get; } = ingredients;

    public int RecipeId { get; } = recipeId;
}