using Microsoft.Extensions.Localization;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

public class HtmlToolResultRenderer(IStringLocalizer<Messages> localizer) : IToolResultRenderer
{
    private const int MaxResultLength = 200;

    private static readonly HashSet<string> SignalToolNames = new(StringComparer.Ordinal)
    {
        "confirm_cart",
        "proceed_to_cart",
    };

    public string RenderToolGroupStart(string groupIcon, string groupLabel)
        => $"{Environment.NewLine}<details class=\"tool-group\"><summary>{groupIcon} {groupLabel}</summary>{Environment.NewLine}";

    public string RenderToolCallStart(string toolName, string formattedArgs)
    {
        if (SignalToolNames.Contains(toolName))
        {
            return string.Empty;
        }

        return $"<details class=\"tool-call\"><summary>🔧 {toolName}({formattedArgs})</summary>{Environment.NewLine}";
    }

    public string RenderToolResult(string toolResult)
    {
        if (toolResult.StartsWith("__phase:", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $"<div class=\"tool-result\">{localizer["ToolResult", Truncate(toolResult, MaxResultLength)]}</div></details>{Environment.NewLine}";
    }

    public string RenderToolGroupEnd()
        => $"</details>{Environment.NewLine}{Environment.NewLine}";

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + "...";
    }
}
