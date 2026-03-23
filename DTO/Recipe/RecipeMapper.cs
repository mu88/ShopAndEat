using DTO.Ingredient;
using EfRecipe = DataLayer.EfClasses.Recipe;

namespace DTO.Recipe;

public static class RecipeMapper
{
    public static ExistingRecipeDto ToDto(this EfRecipe entity)
        => new(entity.Name,
            entity.NumberOfDays,
            entity.NumberOfPersons,
            [..entity.Ingredients.Select(ingredient => ingredient.ToDto())],
            entity.RecipeId);
}
