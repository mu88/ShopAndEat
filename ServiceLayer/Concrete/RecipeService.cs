using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Recipe;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class RecipeService(SimpleCrudHelper simpleCrudHelper, EfCoreContext context) : IRecipeService
{
    public async Task<IReadOnlyList<ExistingRecipeDto>> GetAllRecipesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("RecipeService.GetAllRecipesAsync");
        return (await simpleCrudHelper.GetAllAsDtoAsync<Recipe, ExistingRecipeDto>(recipe => recipe.ToDto(), cancellationToken))
            .OrderBy(recipe => recipe.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task CreateNewRecipeAsync(NewRecipeDto newRecipeDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("RecipeService.CreateNewRecipeAsync");
        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in newRecipeDto.Ingredients)
        {
            var unit = await simpleCrudHelper.FindAsync<Unit>(newIngredientDto.Unit.UnitId, cancellationToken);
            var article = await simpleCrudHelper.FindAsync<Article>(newIngredientDto.Article.ArticleId, cancellationToken);
            newIngredients.Add(context.Ingredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit)).Entity);
        }

        var newRecipe = new Recipe(newRecipeDto.Name, newRecipeDto.NumberOfDays, newRecipeDto.NumberOfPersons, newIngredients);
        context.Recipes.Add(newRecipe);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRecipeAsync(DeleteRecipeDto recipeToDelete, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("RecipeService.DeleteRecipeAsync");

        // Meals referencing this recipe are removed automatically: Meal.Recipe is a required navigation
        // configured with DeleteBehavior.Cascade (see EfCoreContextModelSnapshot / migrations), so the
        // database already cascades the deletion once the recipe itself is removed below. Ingredients are
        // NOT covered by a cascade (Ingredient.RecipeId is an optional, ClientSetNull foreign key), so they
        // still need to be deleted explicitly.
        var existingRecipe = await simpleCrudHelper.FindAsync<Recipe>(recipeToDelete.RecipeId, cancellationToken);

        // Snapshot the IDs before deleting: each DeleteAsync call triggers EF Core's navigation fixup,
        // which removes the deleted dependent from this very Ingredients collection, mutating it while
        // enumerated. Without ToList(), this throws "Collection was modified" for any recipe with more
        // than one ingredient.
        foreach (var ingredientId in existingRecipe.Ingredients.Select(ingredient => ingredient.IngredientId).ToList())
        {
            await simpleCrudHelper.DeleteAsync<Ingredient>(ingredientId, cancellationToken);
        }

        await simpleCrudHelper.DeleteAsync<Recipe>(recipeToDelete.RecipeId, cancellationToken);
    }

    public async Task UpdateRecipeAsync(UpdateRecipeDto existingRecipeDto, CancellationToken cancellationToken = default)
    {
        // Stryker disable once all: verified false negative. RecipeServiceTests.UpdateRecipeAsync_WithNewIngredientsAndChangedProperties_UpdatesRecipeCorrectly
        // asserts on this activity and correctly fails under "dotnet test" (alone and together with its sibling UpdateRecipeAsync tests) when this line is
        // removed; Stryker's mutant-switch test host only reports it as Survived because of an interaction with the process-wide ActivityListener registration.
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("RecipeService.UpdateRecipeAsync");
        var recipe = await simpleCrudHelper.FindAsync<Recipe>(existingRecipeDto.RecipeId, cancellationToken);

        // Resolve/validate the new ingredients BEFORE deleting any old ones: SimpleCrudHelper.DeleteAsync
        // commits each deletion immediately, so if a Unit/Article lookup below threw after old ingredients
        // were already deleted, the recipe would permanently lose them without the new ones ever being
        // added. Building the new list first ensures nothing is deleted unless the whole update can succeed.
        var newIngredients = new List<Ingredient>();
        foreach (var newIngredientDto in existingRecipeDto.Ingredients)
        {
            var unit = await simpleCrudHelper.FindAsync<Unit>(newIngredientDto.Unit.UnitId, cancellationToken);
            var article = await simpleCrudHelper.FindAsync<Article>(newIngredientDto.Article.ArticleId, cancellationToken);
            newIngredients.Add(new Ingredient(article, newIngredientDto.Quantity, unit));
        }

        foreach (var ingredientId in recipe.Ingredients.Select(ingredient => ingredient.IngredientId).ToList())
        {
            await simpleCrudHelper.DeleteAsync<Ingredient>(ingredientId, cancellationToken);
        }

        // Stryker disable once all: equivalent mutant. recipe is already tracked, so assigning newIngredients to its
        // Ingredients navigation in Update() below is enough for EF Core's change tracker to auto-discover and insert
        // them on SaveChangesAsync; verified by running all RecipeServiceTests with this explicit AddRange removed.
        context.Ingredients.AddRange(newIngredients);
        recipe.Update(existingRecipeDto.Name, existingRecipeDto.NumberOfDays, existingRecipeDto.NumberOfPersons, newIngredients);

        await context.SaveChangesAsync(cancellationToken);
    }
}
