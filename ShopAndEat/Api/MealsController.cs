using DTO.Meal;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer;

namespace ShopAndEat.Api;

[Route("api/[controller]")]
[ApiController]
public class MealsController(IMealService mealService) : ControllerBase
{
    [HttpGet("mealsForToday")]
    public IEnumerable<ExistingMealDto> GetMealsForToday() => mealService.GetMealsForToday();
}