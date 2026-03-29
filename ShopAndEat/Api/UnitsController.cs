using DataLayer.EF;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShopAndEat.Api;

[ApiController]
[Route("api/units")]
public class UnitsController(EfCoreContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<Ok<IReadOnlyList<string>>> GetUnits(CancellationToken cancellationToken = default)
    {
        var units = await dbContext.Units
            .OrderBy(unit => unit.Name)
            .Select(unit => unit.Name)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<string>>(units);
    }
}
