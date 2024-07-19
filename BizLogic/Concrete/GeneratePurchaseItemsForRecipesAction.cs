using DataLayer.EfClasses;

namespace BizLogic.Concrete;

public class GeneratePurchaseItemsForRecipesAction : IGeneratePurchaseItemsForRecipesAction
{
    /// <inheritdoc />
    public IEnumerable<PurchaseItem> GeneratePurchaseItems(IEnumerable<(Recipe recipe, int numberOfPersons)> recipesAndPersons)
    {
        var purchaseItems = new List<PurchaseItem>();
        foreach (var (recipe, numberOfPerson) in recipesAndPersons)
        {
            var personQuantifier = (double)numberOfPerson / recipe.NumberOfPersons;
            var dayQuantifier = 1 / (double)recipe.NumberOfDays;
            foreach (var ingredient in recipe.Ingredients)
            {
                purchaseItems.Add(new PurchaseItem(ingredient.Article, ingredient.Quantity * personQuantifier * dayQuantifier, ingredient.Unit));
            }
        }

        return purchaseItems.GroupBy(item => new { item.Article, item.Unit })
            .Select(y => new PurchaseItem(y.Key.Article, y.Sum(z => z.Quantity), y.Key.Unit));
    }
}