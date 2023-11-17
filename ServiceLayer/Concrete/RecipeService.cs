using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.Recipe;

namespace ServiceLayer.Concrete;

public class RecipeService : IRecipeService
{
    public RecipeService(SimpleCrudHelper simpleCrudHelper, EfCoreContext context)
    {
        SimpleCrudHelper = simpleCrudHelper;
        Context = context;
    }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    private EfCoreContext Context { get; }

    /// <inheritdoc />
    public IEnumerable<ExistingRecipeDto> GetAllRecipes()
    {
        return SimpleCrudHelper.GetAllAsDto<Recipe, ExistingRecipeDto>().OrderBy(recipe => recipe.Name);
    }

    /// <inheritdoc />
    public void CreateNewRecipe(NewRecipeDto newRecipeDto)
    {
        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in newRecipeDto.Ingredients)
        {
            var unit = SimpleCrudHelper.Find<Unit>(newIngredientDto.Unit.UnitId);
            var article = SimpleCrudHelper.Find<Article>(newIngredientDto.Article.ArticleId);
            newIngredients.Add(Context.Ingredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit)).Entity);
        }

        var newRecipe = new Recipe(newRecipeDto.Name, newRecipeDto.NumberOfDays, newRecipeDto.NumberOfPersons, newIngredients);
        Context.Recipes.Add(newRecipe);
        Context.SaveChanges();
    }

    /// <inheritdoc />
    public void DeleteRecipe(DeleteRecipeDto recipeToDelete)
    {
        var existingMeals = SimpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>();
        existingMeals.Where(x => x.Recipe.RecipeId == recipeToDelete.RecipeId)
            .ToList()
            .ForEach(x => SimpleCrudHelper.Delete<Meal>(x.MealId));
        var existingRecipe = SimpleCrudHelper.Find<Recipe>(recipeToDelete.RecipeId);
        existingRecipe.Ingredients.Select(x => x.IngredientId).ToList().ForEach(x => SimpleCrudHelper.Delete<Ingredient>(x));
        SimpleCrudHelper.Delete<Recipe>(recipeToDelete.RecipeId);
        Context.SaveChanges();
    }

    public void UpdateRecipe(UpdateRecipeDto existingRecipeDto)
    {
        var recipe = SimpleCrudHelper.Find<Recipe>(existingRecipeDto.RecipeId);

        foreach (var ingredientId in recipe.Ingredients.Select(x=>x.IngredientId).ToList())
        {
            SimpleCrudHelper.Delete<Ingredient>(ingredientId);
        }

        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in existingRecipeDto.Ingredients)
        {
            var unit = SimpleCrudHelper.Find<Unit>(newIngredientDto.Unit.UnitId);
            var article = SimpleCrudHelper.Find<Article>(newIngredientDto.Article.ArticleId);
            newIngredients.Add(Context.Ingredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit)).Entity);
        }

        recipe.Name = existingRecipeDto.Name;
        recipe.NumberOfDays = existingRecipeDto.NumberOfDays;
        recipe.NumberOfPersons = existingRecipeDto.NumberOfPersons;
        recipe.Ingredients = newIngredients;

        Context.SaveChanges();
    }
}