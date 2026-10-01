using DTO.Meal;
using DTO.PurchaseItem;
using DTO.Store;

namespace ServiceLayer;

public interface IMealService
{
    Task CreateMealAsync(NewMealDto newMealDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingMealDto>> GetFutureMealsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingMealDto>> GetMealsForTodayAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewPurchaseItemDto>> GetOrderedPurchaseItemsAsync(ExistingStoreDto existingStoreDto, CancellationToken cancellationToken = default);

    Task DeleteMealAsync(DeleteMealDto mealToDelete, CancellationToken cancellationToken = default);

    Task ToggleMealAsync(int mealId, CancellationToken cancellationToken = default);
}
