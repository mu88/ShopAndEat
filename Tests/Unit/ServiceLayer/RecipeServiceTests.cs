using System.Diagnostics;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Meal;
using DTO.Recipe;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;
using UnitDto = DTO.Unit.ExistingUnitDto;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class RecipeServiceTests
{
    private readonly List<Activity> _completedActivities = [];
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ServiceLayerDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

    [Test]
    public async Task GetAllRecipesAsync_WithNoRecipes_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = CreateTestee(context);

        // Act
        var results = await testee.GetAllRecipesAsync();

        // Assert
        results.Should().BeEmpty();
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "RecipeService.GetAllRecipesAsync");
    }

    [Test]
    public async Task GetAllRecipesAsync_WithMultipleRecipes_ReturnsAllRecipesOrderedByName()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.Recipes.AddRange(
            new Recipe("Zebra Pasta", 1, 2, Enumerable.Empty<Ingredient>()),
            new Recipe("Apple Cake", 1, 4, Enumerable.Empty<Ingredient>()),
            new Recipe("Banana Bread", 1, 3, Enumerable.Empty<Ingredient>()));
        await context.SaveChangesAsync();
        var testee = CreateTestee(context);

        // Act
        var results = (await testee.GetAllRecipesAsync()).ToList();

        // Assert
        results.Should().HaveCount(3).And.SatisfyRespectively(
            first => first.Name.Should().Be("Apple Cake"),
            second => second.Name.Should().Be("Banana Bread"),
            third => third.Name.Should().Be("Zebra Pasta"));
    }

    [Test]
    public async Task CreateNewRecipeAsync_WithValidRecipeAndIngredients_CreatesRecipeWithIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        var existingArticleGroupDto = new ExistingArticleGroupDto(article.ArticleGroup.ArticleGroupId, article.ArticleGroup.Name);
        var existingArticleDto = new ExistingArticleDto(article.ArticleId, article.Name, existingArticleGroupDto, article.IsInventory);
        var existingUnitDto = new UnitDto(unit.Entity.UnitId, unit.Entity.Name);
        var newIngredientDto = new NewIngredientDto(existingArticleDto, 2.0, existingUnitDto);
        var newRecipeDto = new NewRecipeDto("Cheese Omelette", 1, 2, new[] { newIngredientDto });

        var testee = CreateTestee(context);

        // Act
        await testee.CreateNewRecipeAsync(newRecipeDto);

        // Assert
        context.Recipes.Should().ContainSingle(recipe => recipe.Name == "Cheese Omelette");
        var createdRecipe = context.Recipes.First(r => r.Name == "Cheese Omelette");
        createdRecipe.Ingredients.Should().HaveCount(1);
        createdRecipe.NumberOfDays.Should().Be(1);
        createdRecipe.NumberOfPersons.Should().Be(2);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "RecipeService.CreateNewRecipeAsync");
    }

    [Test]
    public async Task CreateNewRecipeAsync_WithMultipleIngredients_CreatesRecipeWithAllIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit1 = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        var unit2 = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var article1 = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        var article2 = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        var existingArticleGroupDto1 = new ExistingArticleGroupDto(article1.ArticleGroup.ArticleGroupId, article1.ArticleGroup.Name);
        var existingArticleGroupDto2 = new ExistingArticleGroupDto(article2.ArticleGroup.ArticleGroupId, article2.ArticleGroup.Name);
        var article1Dto = new ExistingArticleDto(article1.ArticleId, article1.Name, existingArticleGroupDto1, article1.IsInventory);
        var article2Dto = new ExistingArticleDto(article2.ArticleId, article2.Name, existingArticleGroupDto2, article2.IsInventory);
        var unit1Dto = new UnitDto(unit1.Entity.UnitId, unit1.Entity.Name);
        var unit2Dto = new UnitDto(unit2.Entity.UnitId, unit2.Entity.Name);

        var ingredient1 = new NewIngredientDto(article1Dto, 2.0, unit1Dto);
        var ingredient2 = new NewIngredientDto(article2Dto, 500.0, unit2Dto);
        var newRecipeDto = new NewRecipeDto("Cheese Sauce", 1, 4, new[] { ingredient1, ingredient2 });

        var testee = CreateTestee(context);

        // Act
        await testee.CreateNewRecipeAsync(newRecipeDto);

        // Assert
        context.Recipes.Should().ContainSingle(recipe => recipe.Name == "Cheese Sauce");
        var createdRecipe = context.Recipes.First(r => r.Name == "Cheese Sauce");
        createdRecipe.Ingredients.Should().HaveCount(2);
    }

    [Test]
    public async Task CreateNewRecipeAsync_WithEmptyIngredients_CreatesRecipeWithNoIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var newRecipeDto = new NewRecipeDto("Empty Recipe", 1, 2, Enumerable.Empty<NewIngredientDto>());
        var testee = CreateTestee(context);

        // Act
        await testee.CreateNewRecipeAsync(newRecipeDto);

        // Assert
        context.Recipes.Should().ContainSingle(recipe => recipe.Name == "Empty Recipe");
        var createdRecipe = context.Recipes.First(r => r.Name == "Empty Recipe");
        createdRecipe.Ingredients.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteRecipeAsync_WithNoMealsAndNoIngredients_DeletesRecipe()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var recipe = context.Recipes.Add(new Recipe("Pasta", 1, 2, Enumerable.Empty<Ingredient>())); // kept: needs an empty ingredient list, which RecipeBuilder's defaults don't provide
        await context.SaveChangesAsync();
        var deleteRecipeDto = new DeleteRecipeDto(recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Recipes.Should().NotContain(recipe.Entity);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "RecipeService.DeleteRecipeAsync");
    }

    [Test]
    public async Task DeleteRecipeAsync_WithIngredients_DeletesRecipeAndIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        var ingredients = new List<Ingredient>
        {
            new(article, 1.0, unit.Entity), new(article, 2.0, unit.Entity)
        };

        // Recipe kept as direct construction: this specific (non-empty, multi-item) ingredient list is
        // what previously made DeleteRecipeAsync throw "Collection was modified" (missing ToList()
        // snapshot before deleting while enumerating) — this test guards against that regression.
        var recipe = context.Recipes.Add(new Recipe("Cheese Dish", 1, 2, ingredients));
        await context.SaveChangesAsync();
        var ingredientIds = recipe.Entity.Ingredients.Select(ingredient => ingredient.IngredientId).ToList();

        var deleteRecipeDto = new DeleteRecipeDto(recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Recipes.Should().NotContain(recipe.Entity);
        context.Ingredients.Select(ingredient => ingredient.IngredientId).Should().NotContain(ingredientIds);
    }

    [Test]
    public async Task DeleteRecipeAsync_WithAssociatedMeals_DeletesRecipeMealsAndIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var mealType = context.MealTypes.Add(new MealTypeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var recipe = context.Recipes.Add(new Recipe("Omelette", 1, 2, Enumerable.Empty<Ingredient>()));
        await context.SaveChangesAsync();

        var meal1 = context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, recipe.Entity, 2));
        var meal2 = context.Meals.Add(new Meal(DateTime.Today.AddDays(1), mealType.Entity, recipe.Entity, 2));
        await context.SaveChangesAsync();

        var deleteRecipeDto = new DeleteRecipeDto(recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Recipes.Should().NotContain(recipe.Entity);
        context.Meals.Should().NotContain(meal1.Entity).And.NotContain(meal2.Entity);
    }

    [Test]
    public async Task DeleteRecipeAsync_WithRealDatabaseCascade_DeletesMealsViaDbConstraint()
    {
        // Arrange — unlike the InMemory-provider tests above, this uses a real Sqlite database and
        // clears the change tracker before acting, so the Meal row is NOT already tracked in this
        // DbContext instance. This forces DeleteRecipeAsync's recipe deletion to rely entirely on the
        // database's own ON DELETE CASCADE constraint (Meal.Recipe is configured with
        // DeleteBehavior.Cascade) to remove the dependent Meal row, rather than EF Core's client-side
        // tracked-entity cascade fixup, which would mask a misconfigured/missing DB constraint.
        await using var context = new SqliteDbContext();
        var mealType = context.MealTypes.Add(new MealTypeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var recipe = context.Recipes.Add(new Recipe("Omelette", 1, 2, Enumerable.Empty<Ingredient>()));
        await context.SaveChangesAsync();

        var meal = context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, recipe.Entity, 2));
        await context.SaveChangesAsync();
        var mealId = meal.Entity.MealId;
        var recipeId = recipe.Entity.RecipeId;

        context.ChangeTracker.Clear();
        var deleteRecipeDto = new DeleteRecipeDto(recipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Recipes.AsNoTracking().Any(r => r.RecipeId == recipeId).Should().BeFalse();
        context.Meals.AsNoTracking().Any(m => m.MealId == mealId).Should().BeFalse();
    }

    [Test]
    public async Task DeleteRecipeAsync_WithMealsOfOtherRecipes_LeavesOtherRecipesMealsUntouched()
    {
        // Arrange — proves that deleting a recipe only cascades its own meals, not meals of other
        // recipes. Note: this test runs against the InMemory provider, where the "cascade" observed
        // here is really EF's client-side tracked-entity fixup, not the real DB's ON DELETE CASCADE
        // constraint; see DeleteRecipeAsync_WithRealDatabaseCascade_DeletesMealsViaDbConstraint below
        // for a test that exercises the actual Sqlite-enforced cascade.
        await using var context = new InMemoryDbContext();
        var mealType = context.MealTypes.Add(new MealTypeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var recipeToDelete = context.Recipes.Add(new Recipe("Omelette", 1, 2, Enumerable.Empty<Ingredient>()));
        var otherRecipe = context.Recipes.Add(new Recipe("Salad", 1, 2, Enumerable.Empty<Ingredient>()));
        await context.SaveChangesAsync();

        var mealToDelete = context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, recipeToDelete.Entity, 2));
        var otherMeal = context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, otherRecipe.Entity, 2));
        await context.SaveChangesAsync();

        var deleteRecipeDto = new DeleteRecipeDto(recipeToDelete.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Meals.Should().NotContain(mealToDelete.Entity).And.Contain(otherMeal.Entity);
        context.Recipes.Should().Contain(otherRecipe.Entity);
    }

    [Test]
    public async Task DeleteRecipeAsync_WithMealsAndIngredients_DeletesRecipeMealsAndIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        var mealType = context.MealTypes.Add(new MealTypeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        var ingredients = new List<Ingredient>
        {
            new(article, 1.0, unit.Entity), new(article, 2.0, unit.Entity)
        };

        // Recipe kept as direct construction: this specific (non-empty, multi-item) ingredient list is
        // what previously made DeleteRecipeAsync throw "Collection was modified" (missing ToList()
        // snapshot before deleting while enumerating) — this test guards against that regression.
        var recipe = context.Recipes.Add(new Recipe("Cheese Omelette", 1, 2, ingredients));
        await context.SaveChangesAsync();
        var ingredientIds = recipe.Entity.Ingredients.Select(ingredient => ingredient.IngredientId).ToList();

        var meal = context.Meals.Add(new Meal(DateTime.Today, mealType.Entity, recipe.Entity, 2));
        await context.SaveChangesAsync();

        var deleteRecipeDto = new DeleteRecipeDto(recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.DeleteRecipeAsync(deleteRecipeDto);

        // Assert
        context.Recipes.Should().NotContain(recipe.Entity);
        context.Meals.Should().NotContain(meal.Entity);
        context.Ingredients.Select(ingredient => ingredient.IngredientId).Should().NotContain(ingredientIds);
    }

    [Test]
    public async Task UpdateRecipeAsync_WithNewIngredientsAndChangedProperties_UpdatesRecipeCorrectly()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        // Recipe's initial ingredients are irrelevant: UpdateRecipeAsync deletes all of them
        // regardless before adding the new one built from the DTO below.
        var recipe = context.Recipes.Add(new RecipeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var existingArticleGroupDto = new ExistingArticleGroupDto(article.ArticleGroup.ArticleGroupId, article.ArticleGroup.Name);
        var existingArticleDto = new ExistingArticleDto(article.ArticleId, article.Name, existingArticleGroupDto, article.IsInventory);
        var existingUnitDto = new UnitDto(unit.Entity.UnitId, unit.Entity.Name);
        var newIngredientDto = new NewIngredientDto(existingArticleDto, 3.0, existingUnitDto);

        var updateRecipeDto =
            new UpdateRecipeDto("New Dish", 2, 4, new[] { newIngredientDto }, recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.UpdateRecipeAsync(updateRecipeDto);

        // Assert
        var updatedRecipe = context.Recipes.First(r => r.RecipeId == recipe.Entity.RecipeId);
        updatedRecipe.Name.Should().Be("New Dish");
        updatedRecipe.NumberOfDays.Should().Be(2);
        updatedRecipe.NumberOfPersons.Should().Be(4);
        updatedRecipe.Ingredients.Should().HaveCount(1);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "RecipeService.UpdateRecipeAsync");
    }

    [Test]
    public async Task UpdateRecipeAsync_RemovingAllIngredientsAndAddingNew_ReplacesIngredientsList()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var unit1 = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        var unit2 = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var article1 = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        var article2 = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        // Exactly 2 old ingredients on this recipe is essential: the assertion below checks the net
        // change in total ingredient count (-2 old +1 new = -1), so this count can't be arbitrary.
        var oldIngredients =
            new List<Ingredient> { new(article1, 1.0, unit1.Entity), new(article1, 2.0, unit1.Entity) };
        var recipe = context.Recipes.Add(new Recipe("Updated Recipe", 1, 2, oldIngredients));
        await context.SaveChangesAsync();
        var initialIngredientCount = context.Ingredients.Count();

        var existingArticleGroupDto = new ExistingArticleGroupDto(article2.ArticleGroup.ArticleGroupId, article2.ArticleGroup.Name);
        var article2Dto = new ExistingArticleDto(article2.ArticleId, article2.Name, existingArticleGroupDto, article2.IsInventory);
        var unit2Dto = new UnitDto(unit2.Entity.UnitId, unit2.Entity.Name);
        var newIngredientDto = new NewIngredientDto(article2Dto, 500.0, unit2Dto);

        var updateRecipeDto =
            new UpdateRecipeDto("Updated Recipe", 1, 2, new[] { newIngredientDto }, recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.UpdateRecipeAsync(updateRecipeDto);

        // Assert
        var updatedRecipe = context.Recipes.First(r => r.RecipeId == recipe.Entity.RecipeId);
        updatedRecipe.Ingredients.Should().HaveCount(1);
        context.Ingredients.Count().Should().Be(initialIngredientCount - 1);
    }

    [Test]
    public async Task UpdateRecipeAsync_WithEmptyIngredientsAndDeleteOld_RemovesAllOldIngredients()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var recipe = context.Recipes.Add(new RecipeBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();

        var updateRecipeDto = new UpdateRecipeDto(
            "No Ingredients Recipe", 1, 2, Enumerable.Empty<NewIngredientDto>(), recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act
        await testee.UpdateRecipeAsync(updateRecipeDto);

        // Assert
        var updatedRecipe = context.Recipes.First(r => r.RecipeId == recipe.Entity.RecipeId);
        updatedRecipe.Ingredients.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateRecipeAsync_WhenNewIngredientReferencesUnknownUnit_LeavesOldIngredientsIntact()
    {
        // Arrange — regression test: the update must validate/resolve all new ingredients BEFORE
        // deleting any old ones, so a failure partway through never leaves the recipe with neither
        // its old nor its new ingredients (SimpleCrudHelper.DeleteAsync commits immediately per call).
        await using var context = new InMemoryDbContext();
        var unit = context.Units.Add(new UnitBuilder().WithDefaults().Build());
        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();

        var oldIngredients = new List<Ingredient> { new(article, 1.0, unit.Entity) };
        var recipe = context.Recipes.Add(new Recipe("Original Recipe", 1, 2, oldIngredients));
        await context.SaveChangesAsync();
        var oldIngredientId = recipe.Entity.Ingredients.Single().IngredientId;

        var existingArticleGroupDto = new ExistingArticleGroupDto(article.ArticleGroup.ArticleGroupId, article.ArticleGroup.Name);
        var existingArticleDto = new ExistingArticleDto(article.ArticleId, article.Name, existingArticleGroupDto, article.IsInventory);
        var unknownUnitDto = new UnitDto(new UnitId(-1), "unknown");
        var newIngredientDto = new NewIngredientDto(existingArticleDto, 2.0, unknownUnitDto);

        var updateRecipeDto =
            new UpdateRecipeDto("Updated Recipe", 1, 2, new[] { newIngredientDto }, recipe.Entity.RecipeId);
        var testee = CreateTestee(context);

        // Act & Assert
        await testee.Invoking(t => t.UpdateRecipeAsync(updateRecipeDto)).Should().ThrowAsync<KeyNotFoundException>();
        context.Ingredients.Select(ingredient => ingredient.IngredientId).Should().Contain(oldIngredientId);
        context.Recipes.First(r => r.RecipeId == recipe.Entity.RecipeId).Ingredients.Should().HaveCount(1);
    }

    private static RecipeService CreateTestee(EfCoreContext context)
        => new(new SimpleCrudHelper(context), context);
}
