using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.Recipe;

namespace ServiceLayer.Concrete;

public class RecipeService(SimpleCrudHelper simpleCrudHelper, EfCoreContext context) : IRecipeService
{
    public IEnumerable<ExistingRecipeDto> GetAllRecipes() =>
        simpleCrudHelper.GetAllAsDto<Recipe, ExistingRecipeDto>().OrderBy(recipe => recipe.Name, StringComparer.Ordinal);

    /// <inheritdoc />
    public void CreateNewRecipe(NewRecipeDto newRecipeDto)
    {
        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in newRecipeDto.Ingredients)
        {
            var unit = simpleCrudHelper.Find<Unit>(newIngredientDto.Unit.UnitId);
            var article = simpleCrudHelper.Find<Article>(newIngredientDto.Article.ArticleId);
            newIngredients.Add(context.Ingredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit)).Entity);
        }

        var newRecipe = new Recipe(newRecipeDto.Name, newRecipeDto.NumberOfDays, newRecipeDto.NumberOfPersons, newIngredients);
        context.Recipes.Add(newRecipe);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public void DeleteRecipe(DeleteRecipeDto recipeToDelete)
    {
        var existingMeals = simpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>();
        existingMeals.Where(x => x.Recipe.RecipeId == recipeToDelete.RecipeId)
            .ToList()
            .ForEach(x => simpleCrudHelper.Delete<Meal>(x.MealId));
        var existingRecipe = simpleCrudHelper.Find<Recipe>(recipeToDelete.RecipeId);
        existingRecipe.Ingredients.Select(x => x.IngredientId).ToList().ForEach(x => simpleCrudHelper.Delete<Ingredient>(x));
        simpleCrudHelper.Delete<Recipe>(recipeToDelete.RecipeId);
        context.SaveChanges();
    }

    public void UpdateRecipe(UpdateRecipeDto existingRecipeDto)
    {
        var recipe = simpleCrudHelper.Find<Recipe>(existingRecipeDto.RecipeId);

        foreach (var ingredientId in recipe.Ingredients.Select(x => x.IngredientId).ToList())
        {
            simpleCrudHelper.Delete<Ingredient>(ingredientId);
        }

        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in existingRecipeDto.Ingredients)
        {
            var unit = simpleCrudHelper.Find<Unit>(newIngredientDto.Unit.UnitId);
            var article = simpleCrudHelper.Find<Article>(newIngredientDto.Article.ArticleId);
            newIngredients.Add(context.Ingredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit)).Entity);
        }

        recipe.Name = existingRecipeDto.Name;
        recipe.NumberOfDays = existingRecipeDto.NumberOfDays;
        recipe.NumberOfPersons = existingRecipeDto.NumberOfPersons;
        recipe.Ingredients = newIngredients;

        context.SaveChanges();
    }
}