using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Store;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class MealService(
    IGeneratePurchaseItemsForRecipesAction generatePurchaseItemsForRecipesAction,
    IOrderPurchaseItemsByStoreAction orderPurchaseItemsByStoreAction,
    IGetRecipesForMealsAction getRecipesForMealsAction,
    EfCoreContext context,
    SimpleCrudHelper simpleCrudHelper,
    TimeProvider timeProvider)
    : IMealService
{
    /// <inheritdoc />
    public async Task CreateMealAsync(NewMealDto newMealDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.CreateMealAsync");

        // No ToEntity() on NewMealDto: creating a Meal requires resolving the referenced Recipe and
        // MealType by ID against the database, which a pure mapper cannot do without a DbContext.
        var recipe = await simpleCrudHelper.FindAsync<Recipe>(newMealDto.Recipe.RecipeId, cancellationToken);
        var mealType = await simpleCrudHelper.FindAsync<MealType>(newMealDto.MealType.MealTypeId, cancellationToken);
        for (var i = 0; i < newMealDto.NumberOfDays; i++)
        {
            var newMeal = new Meal(newMealDto.Day.AddDays(i), mealType, recipe, newMealDto.NumberOfPersons);
            context.Meals.Add(newMeal);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingMealDto>> GetFutureMealsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.GetFutureMealsAsync");
        return (await simpleCrudHelper.GetAllAsDtoAsync<Meal, ExistingMealDto>(meal => meal.ToDto(), cancellationToken))
            .Where(IsInFuture)
            .OrderBy(meal => meal.Day)
            .ThenBy(meal => meal.MealType.Order)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingMealDto>> GetMealsForTodayAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.GetMealsForTodayAsync");
        return (await simpleCrudHelper.GetAllAsDtoAsync<Meal, ExistingMealDto>(meal => meal.ToDto(), cancellationToken))
            .Where(IsToday)
            .OrderBy(meal => meal.MealType.Order)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NewPurchaseItemDto>> GetOrderedPurchaseItemsAsync(
        ExistingStoreDto existingStoreDto,
        CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.GetOrderedPurchaseItemsAsync");
        var today = GetToday();
        var meals = await context.Meals
            .Where(meal => !meal.HasBeenShopped && meal.Day >= today)
            .ToListAsync(cancellationToken);
        var recipes = getRecipesForMealsAction.GetRecipesForMeals(meals);
        var store = await simpleCrudHelper.FindAsync<Store>(existingStoreDto.StoreId, cancellationToken);

        var orderedPurchaseItemsByStore =
            orderPurchaseItemsByStoreAction.OrderPurchaseItemsByStore(store,
                generatePurchaseItemsForRecipesAction
                    .GeneratePurchaseItems(recipes));

        foreach (var meal in meals)
        {
            meal.MarkAsShopped();
        }

        // Conversion is deferred (no ToList() upstream), so this LINQ chain still needs to lazy-load
        // Article/ArticleGroup/Unit/Store.Compartments navigation properties from the tracked entities.
        // Materialize the DTOs here, before SaveChangesAsync() runs change detection and updates entity state.
        var newPurchaseItemDtos = orderedPurchaseItemsByStore.Select(item => item.ToNewDto()).ToList();

        await context.SaveChangesAsync(cancellationToken);

        return newPurchaseItemDtos;
    }

    /// <inheritdoc />
    public async Task DeleteMealAsync(DeleteMealDto mealToDelete, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.DeleteMealAsync");
        await simpleCrudHelper.DeleteAsync<Meal>(mealToDelete.MealId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ToggleMealAsync(int mealId, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("MealService.ToggleMealAsync");
        var meal = await simpleCrudHelper.FindAsync<Meal>(new MealId(mealId), cancellationToken);
        meal.ToggleShopped();
        await context.SaveChangesAsync(cancellationToken);
    }

    private DateTime GetToday() => timeProvider.GetLocalNow().DateTime.Date;

    private bool IsToday(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) == DateOnly.FromDateTime(GetToday());

    private bool IsInFuture(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) >= DateOnly.FromDateTime(GetToday());
}
