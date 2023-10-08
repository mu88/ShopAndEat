using System.Collections.Generic;
using DataLayer.EfClasses;

namespace BizLogic;

public interface IGetRecipesForMealsAction
{
    IEnumerable<(Recipe recipe, int numberOfPersons, int numberOfDays)> GetRecipesForMeals(IEnumerable<Meal> meals);
}