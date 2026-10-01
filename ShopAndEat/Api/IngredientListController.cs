#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using System.Globalization;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.IngredientList;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ServiceLayer;
using ShopAndEat.Api.Resources;

namespace ShopAndEat.Api;

/// <summary>
/// Provides the current ingredient list from the meal plan for the Shopping Agent.
/// </summary>
[ApiController]
[Route("api/shopping/ingredients")]
public class IngredientListController(IMealService mealService, EfCoreContext context, IStringLocalizer<Messages> localizer) : ControllerBase
{
    /// <summary>
    /// Returns the current ingredient list from un-shopped meals,
    /// formatted as a plain-text list for the Shopping Agent.
    /// </summary>
    /// <remarks>
    /// KNOWN CQS/REST VIOLATION: This GET endpoint mutates state via <see cref="mealService.GetOrderedPurchaseItemsAsync"/>,
    /// which calls <c>meal.MarkAsShopped()</c> and <c>SaveChangesAsync()</c> on matching meals. This is a pre-existing
    /// design issue (GET should be side-effect-free) and is documented here for visibility; not refactored at this time.
    /// </remarks>
    [HttpGet]
    public async Task<Results<Ok<IngredientListResponse>, ProblemHttpResult>> GetIngredientList([FromQuery] int? storeId = null, CancellationToken cancellationToken = default)
    {
        var store = storeId.HasValue
            ? await context.Stores.FindAsync([new StoreId(storeId.Value)], cancellationToken)
            : await context.Stores.OrderBy(s => s.StoreId).FirstOrDefaultAsync(cancellationToken);

        if (store == null)
        {
            if (storeId.HasValue)
            {
                return TypedResults.Problem(detail: localizer["StoreNotFound", storeId.Value.ToString(CultureInfo.InvariantCulture)], statusCode: StatusCodes.Status404NotFound);
            }

            // Stryker disable once all: IngredientListResponse.Items already defaults to an empty collection, so an explicit initializer is behaviorally identical.
            return TypedResults.Ok(new IngredientListResponse { Items = [] });
        }

        var storeDto = new DTO.Store.ExistingStoreDto(store.StoreId, store.Name);
        var purchaseItems = (await mealService.GetOrderedPurchaseItemsAsync(storeDto, cancellationToken)).ToList();

        var items = purchaseItems.Select(pi => new IngredientItem
        {
            Text = pi.ToString(),
            Article = pi.Article.Name,
            Quantity = pi.Quantity,
            Unit = pi.Unit.Name,
        });

        return TypedResults.Ok(new IngredientListResponse { Items = items });
    }
}
