using Microsoft.Extensions.AI;
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace ShoppingAgent.Services;

public interface IToolCallDispatcher
{
    WorkflowPhase Phase { get; }

    bool ShouldBreakAfterToolExecution { get; }

    Task<(string Result, bool Success)> DispatchAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct = default);

    IReadOnlyList<(string Key, string Label, string Icon, IReadOnlyList<FunctionCallContent> Tools)> GroupConsecutiveToolCalls(
        IReadOnlyList<FunctionCallContent> toolCalls);

    string FormatArgs(IDictionary<string, object?>? args);

    void ResetWorkflow();

    static (string Key, string Label, string Icon) GetToolGroup(string toolName) => toolName switch
    {
        "search_products" => ("search", "Product Search", "search"),
        "get_product_details" => ("search", "Product Search", "search"),
        "add_to_cart" => ("cart", "Shopping Cart", "cart"),
        "remove_from_cart" => ("cart", "Shopping Cart", "cart"),
        "get_cart_contents" => ("cart", "Shopping Cart", "cart"),
        "navigate_to_cart" => ("cart", "Shopping Cart", "cart"),
        "save_preference" => ("prefs", "Preferences", "hard-drive"),
        "delete_preference" => ("prefs", "Preferences", "hard-drive"),
        "get_preferences" => ("prefs", "Preferences", "hard-drive"),
        "verify_shopping_list" => ("verify", "Cart Verification", "circle-check"),
        "request_clarification" => ("clarify", "Clarification Needed", "question-mark"),
        SignalToolNames.ConfirmCart => ("workflow", "Shopping Plan", "clipboard"),
        SignalToolNames.ProceedToCart => ("workflow", "Shopping Plan", "clipboard"),
        _ => ("other", "Processing", "wrench")
    };
}
