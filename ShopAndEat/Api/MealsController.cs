using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DTO.Meal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer;

namespace ShopAndEat.Api;

[Route("api/meals")]
[ApiController]
public class MealsController(IMealService mealService) : ControllerBase
{
    [HttpGet("mealsForToday")]
    [SuppressMessage("AspNetCoreAnalyzers.Routing", "ASP009:Use kebab-cased urls.", Justification = "Okay for me here, I'm happy")]
    public Ok<IEnumerable<ExistingMealDto>> GetMealsForToday() => TypedResults.Ok(mealService.GetMealsForToday());
}