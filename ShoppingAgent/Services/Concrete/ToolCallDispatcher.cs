using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using ShoppingAgent.Models;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Routes LLM tool calls to the appropriate shop executor or preferences service.
/// Also provides tool grouping logic for the UI.
/// </summary>
public class ToolCallDispatcher(
    IShopToolExecutorFactory shopToolExecutorFactory,
    IPreferencesService preferencesService,
    IShoppingListVerifier shoppingListVerifier,
    IStringLocalizer<Messages> localizer,
    IShoppingWorkflowState workflowState) : IToolCallDispatcher
{
    public WorkflowPhase Phase => workflowState.Phase;

    public bool ShouldBreakAfterToolExecution =>
        workflowState.Phase is WorkflowPhase.AwaitingConfirmation or WorkflowPhase.AwaitingClarification;

    public async Task<(string Result, bool Success)> DispatchAsync(
        FunctionCallContent toolCall, string shopKey, CancellationToken ct = default)
    {
        try
        {
            var result = toolCall.Name switch
            {
                "confirm_cart" => HandleConfirmCart(),
                "proceed_to_cart" => HandleProceedToCart(),
                "request_clarification" => HandleRequestClarification(toolCall),
                _ => await DispatchShopToolAsync(toolCall, shopKey, ct),
            };
            return (result, true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (localizer["ToolError", ex.Message].Value, false);
        }
    }

    public IReadOnlyList<(string Key, string Label, string Icon, IReadOnlyList<FunctionCallContent> Tools)> GroupConsecutiveToolCalls(
        IReadOnlyList<FunctionCallContent> toolCalls)
    {
        var groups = new List<(string Key, string Label, string Icon, List<FunctionCallContent> Tools)>();

        foreach (var tc in toolCalls)
        {
            var (key, label, icon) = IToolCallDispatcher.GetToolGroup(tc.Name);
            if (groups.Count == 0 || !string.Equals(groups[^1].Key, key, StringComparison.Ordinal))
            {
                groups.Add((key, label, icon, new List<FunctionCallContent> { tc }));
            }
            else
            {
                groups[^1].Tools.Add(tc);
            }
        }

        return groups.Select(g => (g.Key, g.Label, g.Icon, (IReadOnlyList<FunctionCallContent>)g.Tools)).ToList();
    }

    public string FormatArgs(IDictionary<string, object> args)
    {
        if (args == null || args.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", args.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public void ResetWorkflow() => workflowState.Reset();

    private static string GetArg(IDictionary<string, object> args, string key)
        => args != null && args.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;

    private string HandleConfirmCart()
    {
        workflowState.MoveToAwaitingConfirmation();
        return "__phase:awaiting_confirmation__";
    }

    private string HandleProceedToCart()
    {
        workflowState.MoveToFillingCart();
        return "__phase:filling_cart__";
    }

    private string HandleRequestClarification(FunctionCallContent toolCall)
    {
        var rawItems = GetArg(toolCall.Arguments, "pending_items");
        var items = string.IsNullOrWhiteSpace(rawItems)
            ? []
            : rawItems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        workflowState.MoveToAwaitingClarification(items);

        var itemList = items.Length > 0
            ? string.Join(", ", items)
            : "the items above";

        return $"AWAITING CLARIFICATION. Unresolved items: {itemList}. " +
               $"INSTRUCTION: Do NOT search for any products NOW. Do NOT call confirm_cart NOW. " +
               $"Wait for the user to reply. " +
               $"When the user replies: " +
               $"(1) Search for every product the user names by calling search_products. " +
               $"(2) Output the COMPLETE updated plan table as text — ALL rows, not just changed ones. " +
               $"(3) Below the table, ask about ALL still-open ❓ items in text. " +
               $"(4) Only THEN call request_clarification for remaining ❓ items. " +
               $"You MUST output the table and questions as text before calling request_clarification. " +
               $"NEVER skip the table output, even if most rows are unchanged.";
    }

    private async Task<string> DispatchShopToolAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct)
    {
        var toolExecutor = shopToolExecutorFactory.GetExecutor(shopKey);
        return toolCall.Name switch
        {
            "search_products" => JsonSerializer.Serialize(await toolExecutor.SearchAsync(
                GetArg(toolCall.Arguments, "search_term"), ct)),

            "get_product_details" => JsonSerializer.Serialize(await toolExecutor.GetProductDetailsAsync(
                GetArg(toolCall.Arguments, "product_url"), ct)),

            "add_to_cart" => await toolExecutor.AddToCartAsync(
                GetArg(toolCall.Arguments, "product_url"),
                int.TryParse(GetArg(toolCall.Arguments, "quantity"), System.Globalization.CultureInfo.InvariantCulture, out var q) ? q : 1,
                ct),

            "remove_from_cart" => await toolExecutor.RemoveFromCartAsync(
                GetArg(toolCall.Arguments, "product_name"),
                GetArg(toolCall.Arguments, "cart_entry_uid"),
                ct),

            "get_cart_contents" => await toolExecutor.GetCartContentsAsync(ct),

            "navigate_to_cart" => await NavigateToCartWithReminderCheckAsync(toolExecutor, shopKey, ct),

            "save_preference" => await SavePreferenceAsync(toolCall, shopKey, ct),

            "delete_preference" => await DeletePreferenceAsync(toolCall, shopKey, ct),

            "verify_shopping_list" => await VerifyShoppingListAsync(toolCall, shopKey, ct),

            _ => localizer["UnknownTool", toolCall.Name].Value,
        };
    }

    private async Task<string> SavePreferenceAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct)
    {
        var scopeArg = GetArg(toolCall.Arguments, "scope");
        var scope = string.IsNullOrEmpty(scopeArg) ? "global" : scopeArg;
        var key = GetArg(toolCall.Arguments, "key");
        var value = GetArg(toolCall.Arguments, "value");

        var storeKey = scope.StartsWith("article:", StringComparison.OrdinalIgnoreCase)
            ? shopKey
            : null;

        await preferencesService.SavePreferenceAsync(new PreferenceDto
        {
            Scope = scope,
            Key = key,
            Value = value,
            StoreKey = storeKey,
        }, ct);

        return localizer["PreferenceSaved"].Value;
    }

    private async Task<string> DeletePreferenceAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct)
    {
        var scopeArg = GetArg(toolCall.Arguments, "scope");
        var scope = string.IsNullOrEmpty(scopeArg) ? "global" : scopeArg;
        var key = GetArg(toolCall.Arguments, "key");

        var storeKey = scope.StartsWith("article:", StringComparison.OrdinalIgnoreCase)
            ? shopKey
            : null;

        var success = await preferencesService.DeletePreferenceAsync(scope, key, storeKey, ct);
        return success ? localizer["PreferenceDeleted"].Value : localizer["PreferenceNotFound"].Value;
    }

    private async Task<string> NavigateToCartWithReminderCheckAsync(IShopToolExecutor toolExecutor, string shopKey, CancellationToken ct)
    {
        var navigationResult = await toolExecutor.NavigateToCartAsync(ct);

        // Reset after cart navigation so the next user message starts a fresh research cycle.
        workflowState.Reset();

        var allPreferences = await preferencesService.GetAllPreferencesAsync(shopKey, ct);
        var reminders = allPreferences.Where(pref => string.Equals(pref.Scope, "reminder", StringComparison.OrdinalIgnoreCase)).ToList();
        if (reminders.Count == 0)
        {
            return navigationResult;
        }

        var reminderList = string.Join(", ", reminders.Select(reminder => reminder.Key));
        return navigationResult + Environment.NewLine + localizer["ReminderGate", reminderList].Value;
    }

    private async Task<string> VerifyShoppingListAsync(FunctionCallContent toolCall, string shopKey, CancellationToken ct)
    {
        var shoppingList = GetArg(toolCall.Arguments, "shopping_list");
        var toolExecutor = shopToolExecutorFactory.GetExecutor(shopKey);
        var cartContents = await toolExecutor.GetCartContentsAsync(ct);

        var missingItems = shoppingListVerifier.FindMissingItems(shoppingList, cartContents);
        if (missingItems.Count == 0)
        {
            return "OK — all items from the shopping list appear to be in the cart.";
        }

        return "Potentially missing from cart: " + string.Join(", ", missingItems) +
               ". Please check and add them before navigating to cart.";
    }
}
