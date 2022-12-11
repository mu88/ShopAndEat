﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Recipe;
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
    public ExistingMealDto CreateMeal(NewMealDto newMealDto)
    {
        // TODO mu88: Try to avoid this manual mapping logic
        var recipe = SimpleCrudHelper.Find<Recipe>(newMealDto.Recipe.RecipeId);
        var mealType = SimpleCrudHelper.Find<MealType>(newMealDto.MealType.MealTypeId);
        var newMeal = new Meal(newMealDto.Day, mealType, recipe, newMealDto.NumberOfPersons);
        var createdMeal = Context.Meals.Add(newMeal);
        Context.SaveChanges();

        return Mapper.Map<ExistingMealDto>(createdMeal.Entity);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingMealDto> GetFutureMeals()
    {
        var allMeals = SimpleCrudHelper.GetAllAsDto<Meal, ExistingMealDto>();

        var results = new Collection<ExistingMealDto>();
        foreach (var meal in allMeals.Where(x => IsInFuture(x.Day)))
        {
            if (meal.Recipe.NumberOfDays > 1)
            {
                for (var i = 0; i < meal.Recipe.NumberOfDays; i++)
                {
                    results.Add(new ExistingMealDto(meal.Day.AddDays(i), meal.MealType, meal.Recipe, meal.MealId, meal.HasBeenShopped));
                }
            }
            else
            {
                results.Add(meal);
            }
        }

        return results.OrderBy(x => x.Day).ThenBy(x => x.MealType.Order);
    }

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

        foreach (var meal in meals)
        {
            meal.HasBeenShopped = true;
        }

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

    private bool IsInFuture(DateTime mealDate)
    {
        // only compare year, month and day - we don't need hours and minutes
        var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        return mealDate >= now;
    }
}