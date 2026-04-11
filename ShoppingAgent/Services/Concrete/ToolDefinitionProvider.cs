#nullable enable
using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Provides AI tool definitions that describe the available shop operations to the LLM.
/// </summary>
public class ToolDefinitionProvider : IToolDefinitionProvider
{
    public IReadOnlyList<AITool> GetToolDefinitions(string shopName)
    {
        return new List<AITool>
        {
            AIFunctionFactory.Create(
                (string search_term) => Task.FromResult(string.Empty),
                "search_products",
                $"Searches the {shopName} online shop for products. Returns a list of products with name, price, and URL."),

            AIFunctionFactory.Create(
                (string product_url) => Task.FromResult(string.Empty),
                "get_product_details",
                $"Opens a product detail page in the {shopName} online shop and returns name, price, unit size, brand, and availability."),

            AIFunctionFactory.Create(
                (string product_url, int quantity) => Task.FromResult(string.Empty),
                "add_to_cart",
                $"Adds a product with the specified quantity to the {shopName} shopping cart. Uses the shop API directly — reliable and fast. For promotional products, the result contains promoAvailable/promoText."),

            AIFunctionFactory.Create(
                (string product_name, string? cart_entry_uid = null) => Task.FromResult(string.Empty),
                "remove_from_cart",
                $"Removes a product from the {shopName} shopping cart. Prefer passing the cart_entry_uid from the add_to_cart result when the product was added in this session — this ensures the correct item is removed. Fall back to product_name (or part of it) only when cart_entry_uid is unavailable."),

            AIFunctionFactory.Create(
                () => Task.FromResult(string.Empty),
                "get_cart_contents",
                $"Returns the current contents of the {shopName} shopping cart. Shows name, quantity, price, and product ID for each item."),

            AIFunctionFactory.Create(
                () => Task.FromResult(string.Empty),
                "navigate_to_cart",
                $"Navigates the {shopName} tab to the shopping cart in the background. The tab is not brought to the foreground. Use this tool at the end so the user sees the cart when switching tabs."),

            AIFunctionFactory.Create(
                (string scope, string key, string value) => Task.FromResult(string.Empty),
                "save_preference",
                "Saves a learned preference for future shopping. Scope: 'global' for general preferences, 'article:<name>' for article-specific, 'reminder' for reminders. Key: e.g. 'confirmed_product', 'prefer_bio', 'search_term'. Value: the stored value."),

            AIFunctionFactory.Create(
                (string scope, string key) => Task.FromResult(string.Empty),
                "delete_preference",
                "Deletes a saved preference. Use this when the user says 'forget Tofu' (scope='article:Tofu', key='confirmed_product') or 'remove X from the reminder list' (scope='reminder', key='X')."),
            AIFunctionFactory.Create(
                (string shopping_list) => Task.FromResult(string.Empty),
                "verify_shopping_list",
                "Verifies that all items from the original shopping list are represented in the current cart. Call this BEFORE navigate_to_cart when processing a shopping list. Pass the original list text exactly as the user provided it. Returns a list of potentially missing items, or 'OK' if everything is accounted for."),
        };
    }
}
