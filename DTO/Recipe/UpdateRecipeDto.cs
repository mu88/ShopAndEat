using DTO.Ingredient;

namespace DTO.Recipe;

public class UpdateRecipeDto
{
    public UpdateRecipeDto(string name,
                           int numberOfDays,
                           int numberOfPersons,
                           IEnumerable<NewIngredientDto> ingredients,
                           int recipeId)
    {
        Name = name;
        NumberOfDays = numberOfDays;
        Ingredients = ingredients;
        RecipeId = recipeId;
        NumberOfPersons = numberOfPersons;
    }

    public string Name { get; }

    public int NumberOfDays { get; }
    
    public int NumberOfPersons { get;  }

    public IEnumerable<NewIngredientDto> Ingredients { get; }

    public int RecipeId { get; }
}