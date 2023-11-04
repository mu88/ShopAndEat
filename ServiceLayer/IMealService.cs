using System.Collections.Generic;
using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Recipe;
using DTO.Store;

namespace ServiceLayer;

public interface IMealService
{
    void CreateMeal(NewMealDto newMealDto);

    IEnumerable<ExistingMealDto> GetFutureMeals();

    IEnumerable<ExistingMealDto> GetMealsForToday();
        
    IEnumerable<NewPurchaseItemDto> GetOrderedPurchaseItems(ExistingStoreDto store);

    void DeleteMeal(DeleteMealDto mealToDelete);

    void ToggleMeal(int mealId);
}