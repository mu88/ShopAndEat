using DTO.Ingredient;

namespace DTO.Recipe;

public record NewRecipeDto(string Name, int NumberOfDays, int NumberOfPersons, IEnumerable<NewIngredientDto> Ingredients);
