using BizDbAccess;
using DTO.OnlineArticleMapping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ShopAndEat.Api;

[ApiController]
[Route("api/shopping")]
public class OnlineShoppingController(IArticleMappingRepository articleMappingRepository) : ControllerBase
{
    [HttpPost("{storeKey}/mappings")]
    public async Task<Created> SaveArticleMapping([FromRoute] string storeKey, [FromBody] NewOnlineArticleMappingDto mappingDto, CancellationToken cancellationToken = default)
    {
        var mapping = mappingDto.ToEntity(storeKey);

        await articleMappingRepository.SaveOrUpdateMappingAsync(mapping, cancellationToken);

        return TypedResults.Created();
    }

    /// <summary>Returns the best (highest confidence) mapping for an article.</summary>
    [HttpGet("{storeKey}/mappings/{articleName}")]
    public async Task<Results<Ok<ExistingOnlineArticleMappingDto>, ProblemHttpResult>> GetArticleMapping([FromRoute] string storeKey, [FromRoute] string articleName, CancellationToken cancellationToken = default)
    {
        var mapping = await articleMappingRepository.GetMappingAsync(storeKey, articleName, cancellationToken);

        if (mapping == null)
        {
            return TypedResults.Problem(detail: $"No mapping found for '{articleName}' in store '{storeKey}'.", statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(mapping.ToDto());
    }

    /// <summary>Returns ALL known mappings for an article, sorted by most-used first.</summary>
    [HttpGet("{storeKey}/mappings/{articleName}/all")]
    public async Task<Ok<IReadOnlyList<ExistingOnlineArticleMappingDto>>> GetAllArticleMappings([FromRoute] string storeKey, [FromRoute] string articleName, CancellationToken cancellationToken = default)
    {
        var mappings = await articleMappingRepository.GetAllMappingsAsync(storeKey, articleName, cancellationToken);

        return TypedResults.Ok<IReadOnlyList<ExistingOnlineArticleMappingDto>>(mappings.Select(mapping => mapping.ToDto()).ToArray());
    }
}
