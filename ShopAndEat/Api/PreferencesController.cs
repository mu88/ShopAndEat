using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.ShoppingPreference;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ShopAndEat.Logging;

namespace ShopAndEat.Api;

[Route("api/preferences")]
[ApiController]
public class PreferencesController(IPreferencesRepository preferencesRepository, ILogger<PreferencesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<Ok<IReadOnlyList<PreferenceResponse>>> GetAll([FromQuery] string scope = null, [FromQuery] string storeKey = null, CancellationToken cancellationToken = default)
    {
        var preferences = await preferencesRepository.GetAllPreferencesAsync(scope, storeKey, cancellationToken);

        return TypedResults.Ok<IReadOnlyList<PreferenceResponse>>(preferences.Select(preference => preference.ToPreferenceResponse()).ToList());
    }

    [HttpPost]
    public async Task<Ok> Upsert([FromBody] PreferenceRequest request, CancellationToken cancellationToken = default)
    {
        await preferencesRepository.UpsertPreferenceAsync(new ShoppingPreference(request.Scope, request.Key, request.Source, request.StoreKey)
        {
            Value = request.Value,
        }, cancellationToken);

        ControllerLogMessages.PreferenceUpserted(logger, request.Scope, request.Key);
        return TypedResults.Ok();
    }

    [HttpDelete]
    public async Task<Results<NoContent, ProblemHttpResult>> Delete([FromQuery] string scope, [FromQuery] string key, [FromQuery] string storeKey = null, CancellationToken cancellationToken = default)
    {
        var deleted = await preferencesRepository.DeletePreferenceAsync(scope, key, storeKey, cancellationToken);

        if (!deleted)
        {
            return TypedResults.Problem(detail: "No preference found for the specified scope, key, and store.", statusCode: StatusCodes.Status404NotFound);
        }

        ControllerLogMessages.PreferenceDeleted(logger, scope, key);
        return TypedResults.NoContent();
    }
}
