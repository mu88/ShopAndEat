using DataLayer.EfClasses;

namespace BizLogic;

public interface IGeneratePurchaseItemsForRecipesAction
{
    IEnumerable<PurchaseItem> GeneratePurchaseItems(IEnumerable<(Recipe recipe, int numberOfPersons)> recipesAndPersons);
}
