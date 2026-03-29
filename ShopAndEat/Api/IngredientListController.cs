#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using System.Globalization;
using DataLayer.EF;
using DTO.IngredientList;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceLayer;

namespace ShopAndEat.Api;

/// <summary>
/// Provides the current ingredient list from the meal plan for the Shopping Agent.
/// </summary>
[ApiController]
[Route("api/shopping/ingredients")]
public class IngredientListController(IMealService mealService, EfCoreContext context) : ControllerBase
{
    /// <summary>
    /// Returns the current ingredient list from un-shopped meals,
    /// formatted as a plain-text list for the Shopping Agent.
    /// </summary>
    [HttpGet]
    public async Task<Results<Ok<IngredientListResponse>, ProblemHttpResult>> GetIngredientList([FromQuery] int? storeId = null, CancellationToken cancellationToken = default)
    {
        var store = storeId.HasValue
            ? await context.Stores.FindAsync([storeId.Value], cancellationToken)
            : await context.Stores.OrderBy(s => s.StoreId).FirstOrDefaultAsync(cancellationToken);

        if (store == null)
        {
            if (storeId.HasValue)
            {
                return TypedResults.Problem(detail: $"Store with ID {storeId.Value.ToString(CultureInfo.InvariantCulture)} was not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.Ok(new IngredientListResponse { Items = [] });
        }

        var storeDto = new DTO.Store.ExistingStoreDto(store.StoreId, store.Name);
        var purchaseItems = mealService.GetOrderedPurchaseItems(storeDto).ToList();

        var items = purchaseItems.Select(pi => new IngredientItem
        {
            Text = pi.ToString(),
            Article = pi.Article?.Name ?? string.Empty,
            Quantity = pi.Quantity,
            Unit = pi.Unit?.Name ?? string.Empty,
        });

        return TypedResults.Ok(new IngredientListResponse { Items = items });
    }
}
