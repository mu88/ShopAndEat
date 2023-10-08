using System.Collections.Generic;
using System.Linq;
using DataLayer.EfClasses;

namespace BizLogic.Concrete;

public class GeneratePurchaseItemsForRecipesAction : IGeneratePurchaseItemsForRecipesAction
{
    /// <inheritdoc />
    public IEnumerable<PurchaseItem> GeneratePurchaseItems(IEnumerable<(Recipe recipe, int numberOfPersons, int numberOfDays)> recipesAndPersons)
    {
        var purchaseItems = new List<PurchaseItem>();
        foreach (var (recipe, numberOfPerson, numberOfDays) in recipesAndPersons)
        {
            var personQuantifier = (double)numberOfPerson / recipe.NumberOfPersons;
            var dayQuantifier = (double)numberOfDays / recipe.NumberOfDays;
            foreach (var ingredient in recipe.Ingredients)
            {
                purchaseItems.Add(new PurchaseItem(ingredient.Article, ingredient.Quantity * personQuantifier * dayQuantifier, ingredient.Unit));
            }
        }
        
        return purchaseItems.GroupBy(item => new { item.Article, item.Unit })
            .Select(y => new PurchaseItem(y.Key.Article, y.Sum(z => z.Quantity), y.Key.Unit));
    }
}