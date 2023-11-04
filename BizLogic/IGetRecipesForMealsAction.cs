using System.Collections.Generic;
using DataLayer.EfClasses;

namespace BizLogic;

public interface IGetRecipesForMealsAction
{
    IEnumerable<(Recipe recipe, int numberOfPersons)> GetRecipesForMeals(IEnumerable<Meal> meals);
}