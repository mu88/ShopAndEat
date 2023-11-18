using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Store;

namespace ServiceLayer;

public interface IMealService
{
    void CreateMeal(NewMealDto newMealDto);

    IEnumerable<ExistingMealDto> GetFutureMeals();

    IEnumerable<ExistingMealDto> GetMealsForToday();
        
    IEnumerable<NewPurchaseItemDto> GetOrderedPurchaseItems(ExistingStoreDto existingStoreDto);

    void DeleteMeal(DeleteMealDto mealToDelete);

    void ToggleMeal(int mealId);
}