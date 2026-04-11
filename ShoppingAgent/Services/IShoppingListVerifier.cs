namespace ShoppingAgent.Services;

/// <summary>
/// Verifies that all items from an original shopping list are represented in the current cart.
/// Uses keyword matching — intentionally simple to keep false negatives low.
/// </summary>
public interface IShoppingListVerifier
{
    /// <summary>
    /// Returns the item names from <paramref name="shoppingList"/> that could not be matched
    /// against any entry in <paramref name="cartContents"/>.
    /// An empty result means all items appear to be accounted for.
    /// </summary>
    IReadOnlyList<string> FindMissingItems(string shoppingList, string cartContents);
}
