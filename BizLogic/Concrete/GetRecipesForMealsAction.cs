using System.Collections.Generic;
using System.Linq;
using DataLayer.EfClasses;
using DTO.Meal;

namespace BizLogic.Concrete;

public class GetRecipesForMealsAction : IGetRecipesForMealsAction
{
    /// <inheritdoc />
    public IEnumerable<(Recipe recipe, int numberOfPersons, int numberOfDays)> GetRecipesForMeals(IEnumerable<Meal> meals)
    {
        return meals.Select(x => (x.Recipe, x.NumberOfPersons, x.NumberOfDays));
    }
}