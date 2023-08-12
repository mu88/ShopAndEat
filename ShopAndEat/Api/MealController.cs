using System.Collections.Generic;
using DTO.Meal;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer;

namespace ShopAndEat.Api;

[Route("api/[controller]")]
[ApiController]
public class MealController : ControllerBase
{
    private readonly IMealService _mealService;

    public MealController(IMealService mealService) => _mealService = mealService;

    [HttpGet("mealsForToday")]
    public IEnumerable<ExistingMealDto> GetMealsForToday() => _mealService.GetMealsForToday();
}