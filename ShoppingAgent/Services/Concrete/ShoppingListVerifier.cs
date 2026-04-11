using System.Text.RegularExpressions;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Matches shopping list items against cart contents using keyword extraction and case-insensitive substring matching.
/// The algorithm strips leading quantity/unit tokens from each line (e.g. "3 Packungen", "75 Gramm", "1 Stück")
/// and checks whether any cart item name contains at least one significant word from the remaining item keyword.
/// False positives (flagging an item that is actually present) are acceptable — the LLM resolves ambiguity.
/// False negatives (missing a truly absent item) are what this verifier prevents.
/// </summary>
public partial class ShoppingListVerifier : IShoppingListVerifier
{
    public IReadOnlyList<string> FindMissingItems(string shoppingList, string cartContents)
    {
        if (string.IsNullOrWhiteSpace(shoppingList) || string.IsNullOrWhiteSpace(cartContents))
        {
            return [];
        }

        var cartLower = cartContents.ToLowerInvariant();
        var missing = new List<string>();

        foreach (var line in shoppingList.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var keyword = LeadingQuantityPattern().Replace(trimmed, string.Empty).Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }

            // Strip parenthetical notes at end, e.g. "Hähnchenfleisch (Poulet)" → "Hähnchenfleisch"
            var parenIndex = keyword.IndexOf('(', StringComparison.Ordinal);
            var baseKeyword = parenIndex > 0 ? keyword[..parenIndex].Trim() : keyword;

            if (!IsFoundInCart(baseKeyword, cartLower))
            {
                missing.Add(keyword);
            }
        }

        return missing;
    }

    private static bool IsFoundInCart(string keyword, string cartLower)
    {
        // A keyword is "found" when at least one significant word (length > 3) appears in the cart text.
        var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => word.Length > 3 && cartLower.Contains(word.ToLowerInvariant(), StringComparison.Ordinal));
    }

    // Matches leading patterns like "3 Packungen", "75 Gramm", "1 Stück (klein)", "2x", etc.
    // MA0009 suppressed: [GeneratedRegex] produces source-generated, non-backtracking code that cannot exhibit ReDoS.
#pragma warning disable MA0009
    [GeneratedRegex(
        @"^\d+[\.,]?\d*\s*(?:x\s*)?(?:Packung(?:en)?|Stück|Pack|Dosen?|Flaschen?|Glas|Gläser|Gramm|Kilogramm|kg|g|Liter|Milliliter|ml|l|Bund|Blatt|Scheiben?|Portion(?:en)?|St\b)?\.?\s*(?:\([^)]*\))?\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    private static partial Regex LeadingQuantityPattern();
#pragma warning restore MA0009
}
