using System.Collections.ObjectModel;
using AutoMapper;
using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Store;

namespace ServiceLayer.Concrete;

public class MealService(
    IGeneratePurchaseItemsForRecipesAction generatePurchaseItemsForRecipesAction,
    IOrderPurchaseItemsByStoreAction orderPurchaseItemsByStoreAction,
    IGetRecipesForMealsAction getRecipesForMealsAction,
    EfCoreContext context,
    SimpleCrudHelper simpleCrudHelper,
    IMapper mapper)
    : IMealService
{

    /// <inheritdoc />
    public void CreateMeal(NewMealDto newMealDto)
    {
        // TODO mu88: Try to avoid this manual mapping logic
        var recipe = simpleCrudHelper.Find<Recipe>(newMealDto.Recipe.RecipeId);
        var mealType = simpleCrudHelper.Find<MealType>(newMealDto.MealType.MealTypeId);
        for (var i = 0; i < newMealDto.NumberOfDays; i++)
        {
            var newMeal = new Meal(newMealDto.Day.AddDays(i), mealType, recipe, newMealDto.NumberOfPersons);
            context.Meals.Add(newMeal);
        }
        context.SaveChanges();
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealDto> GetFutureMeals()
    {
        var allMeals = simpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>();

        var results = new Collection<ExistingMealDto>();
        foreach (var meal in allMeals.Where(IsInFuture))
        {
            results.Add(meal);
        }

        return results.OrderBy(x => x.Day).ThenBy(x => x.MealType.Order);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealDto> GetMealsForToday() => simpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>()
        .Where(IsToday)
        .OrderBy(meal => meal.MealType.Order);

    /// <inheritdoc />
    public IEnumerable<NewPurchaseItemDto> GetOrderedPurchaseItems(ExistingStoreDto existingStoreDto)
    {
        var meals = context.Meals.Where(x => !x.HasBeenShopped);
        var recipes = getRecipesForMealsAction.GetRecipesForMeals(meals);
        var store = simpleCrudHelper.Find<Store>(existingStoreDto.StoreId);

        var orderedPurchaseItemsByStore =
            orderPurchaseItemsByStoreAction.OrderPurchaseItemsByStore(store,
                                                                      generatePurchaseItemsForRecipesAction
                                                                          .GeneratePurchaseItems(recipes));

        foreach (var meal in meals) { meal.HasBeenShopped = true; }

        var newPurchaseItemDtos = mapper.Map<IEnumerable<NewPurchaseItemDto>>(orderedPurchaseItemsByStore);

        // TODO MUL: Investigate why conversion has to be done before calling SaveChanges()
        context.SaveChanges();

        return newPurchaseItemDtos;
    }

    /// <inheritdoc />
    public void DeleteMeal(DeleteMealDto mealToDelete)
    {
        simpleCrudHelper.Delete<Meal>(mealToDelete.MealId);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public void ToggleMeal(int mealId)
    {
        var meal = simpleCrudHelper.Find<Meal>(mealId);
        meal.HasBeenShopped = !meal.HasBeenShopped;
        context.SaveChanges();
    }

    private static bool IsToday(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) == DateOnly.FromDateTime(DateTime.Today);

    private static bool IsInFuture(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) >= DateOnly.FromDateTime(DateTime.Today);
}