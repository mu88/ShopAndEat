using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public interface IToolCallDispatcher
{
    Task<(string Result, bool Success)> DispatchAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct = default);

    List<(string Key, string Label, string Icon, List<FunctionCallContent> Tools)> GroupConsecutiveToolCalls(
        List<FunctionCallContent> toolCalls);

    string FormatArgs(IDictionary<string, object> args);

    static (string Key, string Label, string Icon) GetToolGroup(string toolName) => toolName switch
    {
        "search_products" => ("search", "Product Search", "🔍"),
        "get_product_details" => ("search", "Product Search", "🔍"),
        "add_to_cart" => ("cart", "Shopping Cart", "🛒"),
        "remove_from_cart" => ("cart", "Shopping Cart", "🛒"),
        "get_cart_contents" => ("cart", "Shopping Cart", "🛒"),
        "navigate_to_cart" => ("cart", "Shopping Cart", "🛒"),
        "save_preference" => ("prefs", "Preferences", "💾"),
        "delete_preference" => ("prefs", "Preferences", "💾"),
        "get_preferences" => ("prefs", "Preferences", "💾"),
        _ => ("other", "Processing", "🔧")
    };
}
