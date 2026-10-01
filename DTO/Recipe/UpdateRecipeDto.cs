using DataLayer.EfClasses;
using DTO.Ingredient;

namespace DTO.Recipe;

public record UpdateRecipeDto(
    string Name,
    int NumberOfDays,
    int NumberOfPersons,
    IEnumerable<NewIngredientDto> Ingredients,
    RecipeId RecipeId);
