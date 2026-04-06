using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Routes LLM tool calls to the appropriate shop executor or preferences service.
/// Also provides tool grouping logic for the UI.
/// </summary>
public class ToolCallDispatcher(
    IShopToolExecutorFactory shopToolExecutorFactory,
    IPreferencesService preferencesService,
    IStringLocalizer<Messages> localizer) : IToolCallDispatcher
{
    public async Task<(string Result, bool Success)> DispatchAsync(
        FunctionCallContent toolCall, string shopKey, CancellationToken ct = default)
    {
        try
        {
            var toolExecutor = shopToolExecutorFactory.GetExecutor(shopKey);
            var result = toolCall.Name switch
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
                    GetArg(toolCall.Arguments, "product_name"), ct),

                "get_cart_contents" => await toolExecutor.GetCartContentsAsync(ct),

                "navigate_to_cart" => await toolExecutor.NavigateToCartAsync(ct),

                "save_preference" => await SavePreferenceAsync(toolCall, shopKey, ct),

                "delete_preference" => await DeletePreferenceAsync(toolCall, shopKey, ct),

                _ => localizer["UnknownTool", toolCall.Name].Value
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

    private static string GetArg(IDictionary<string, object> args, string key)
        => args != null && args.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : string.Empty;

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
}
