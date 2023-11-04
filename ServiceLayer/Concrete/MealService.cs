using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Store;

namespace ServiceLayer.Concrete;

public class MealService : IMealService
{
    public MealService(IGeneratePurchaseItemsForRecipesAction generatePurchaseItemsForRecipesAction,
                       IOrderPurchaseItemsByStoreAction orderPurchaseItemsByStoreAction,
                       IGetRecipesForMealsAction getRecipesForMealsAction,
                       EfCoreContext context,
                       SimpleCrudHelper simpleCrudHelper,
                       IMapper mapper)
    {
        GeneratePurchaseItemsForRecipesAction = generatePurchaseItemsForRecipesAction;
        OrderPurchaseItemsByStoreAction = orderPurchaseItemsByStoreAction;
        Context = context;
        SimpleCrudHelper = simpleCrudHelper;
        Mapper = mapper;
        GetRecipesForMealsAction = getRecipesForMealsAction;
    }

    private IGeneratePurchaseItemsForRecipesAction GeneratePurchaseItemsForRecipesAction { get; }

    private IOrderPurchaseItemsByStoreAction OrderPurchaseItemsByStoreAction { get; }

    private IGetRecipesForMealsAction GetRecipesForMealsAction { get; }

    private EfCoreContext Context { get; }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    private IMapper Mapper { get; }

    /// <inheritdoc />
    public void CreateMeal(NewMealDto newMealDto)
    {
        // TODO mu88: Try to avoid this manual mapping logic
        var recipe = SimpleCrudHelper.Find<Recipe>(newMealDto.Recipe.RecipeId);
        var mealType = SimpleCrudHelper.Find<MealType>(newMealDto.MealType.MealTypeId);
        for (var i = 0; i < newMealDto.NumberOfDays; i++)
        {
            var newMeal = new Meal(newMealDto.Day.AddDays(i), mealType, recipe, newMealDto.NumberOfPersons);
            Context.Meals.Add(newMeal);
        }
        Context.SaveChanges();
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealDto> GetFutureMeals()
    {
        var allMeals = SimpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>();

        var results = new Collection<ExistingMealDto>();
        foreach (var meal in allMeals.Where(IsInFuture))
        {
            results.Add(meal);
        }

        return results.OrderBy(x => x.Day).ThenBy(x => x.MealType.Order);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealDto> GetMealsForToday() => SimpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>()
        .Where(IsToday)
        .OrderBy(meal => meal.MealType.Order);

    /// <inheritdoc />
    public IEnumerable<NewPurchaseItemDto> GetOrderedPurchaseItems(ExistingStoreDto existingStoreDto)
    {
        var meals = Context.Meals.Where(x => !x.HasBeenShopped);
        var recipes = GetRecipesForMealsAction.GetRecipesForMeals(meals);
        var store = SimpleCrudHelper.Find<Store>(existingStoreDto.StoreId);

        var orderedPurchaseItemsByStore =
            OrderPurchaseItemsByStoreAction.OrderPurchaseItemsByStore(store,
                                                                      GeneratePurchaseItemsForRecipesAction
                                                                          .GeneratePurchaseItems(recipes));

        foreach (var meal in meals) { meal.HasBeenShopped = true; }

        var newPurchaseItemDtos = Mapper.Map<IEnumerable<NewPurchaseItemDto>>(orderedPurchaseItemsByStore);

        // TODO MUL: Investigate why conversion has to be done before calling SaveChanges()
        Context.SaveChanges();

        return newPurchaseItemDtos;
    }

    /// <inheritdoc />
    public void DeleteMeal(DeleteMealDto mealToDelete)
    {
        SimpleCrudHelper.Delete<Meal>(mealToDelete.MealId);
        Context.SaveChanges();
    }

    /// <inheritdoc />
    public void ToggleMeal(int mealId)
    {
        var meal = SimpleCrudHelper.Find<Meal>(mealId);
        meal.HasBeenShopped = !meal.HasBeenShopped;
        Context.SaveChanges();
    }

    private static bool IsToday(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) == DateOnly.FromDateTime(DateTime.Today);

    private static bool IsInFuture(ExistingMealDto meal) => DateOnly.FromDateTime(meal.Day) >= DateOnly.FromDateTime(DateTime.Today);
}