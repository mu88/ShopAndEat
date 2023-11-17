using DTO.Ingredient;

namespace DTO.Recipe;

public class ExistingRecipeDto
{
    public ExistingRecipeDto(string name,
                             int numberOfDays,
                             int numberOfPersons,
                             IEnumerable<ExistingIngredientDto> ingredients,
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

    public IEnumerable<ExistingIngredientDto> Ingredients { get; }

    public int RecipeId { get; }
}