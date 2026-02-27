using DataLayer.EfClasses;

namespace BizLogic.Concrete;

public class GetRecipesForMealsAction : IGetRecipesForMealsAction
{
    /// <inheritdoc />
    public IEnumerable<(Recipe recipe, int numberOfPersons)> GetRecipesForMeals(IEnumerable<Meal> meals) => meals.Select(meal => (meal.Recipe, meal.NumberOfPersons));
}
