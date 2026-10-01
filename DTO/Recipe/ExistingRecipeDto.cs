using DataLayer.EfClasses;
using DTO.Ingredient;

namespace DTO.Recipe;

public record ExistingRecipeDto(
    string Name,
    int NumberOfDays,
    int NumberOfPersons,
    IEnumerable<ExistingIngredientDto> Ingredients,
    RecipeId RecipeId);
