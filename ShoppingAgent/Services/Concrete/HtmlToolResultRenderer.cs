using System.Net;
using Microsoft.Extensions.Localization;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.Concrete;

public class HtmlToolResultRenderer(IStringLocalizer<Messages> localizer) : IToolResultRenderer
{
    private const int MaxResultLength = 200;

    private static readonly HashSet<string> _signalToolNames = new(StringComparer.Ordinal)
    {
        SignalToolNames.ConfirmCart,
        SignalToolNames.ProceedToCart,
    };

    // groupIcon/groupLabel originate from IToolCallDispatcher.GetToolGroup's fixed, developer-authored
    // switch expression (never from the LLM or scraped content), so they are trusted and left unencoded.
    public string RenderToolGroupStart(string groupIcon, string groupLabel)
        => $"{Environment.NewLine}<details class=\"tool-group\"><summary>{groupIcon} {groupLabel}</summary>{Environment.NewLine}";

    public string RenderToolCallStart(string toolName, string formattedArgs)
    {
        if (_signalToolNames.Contains(toolName))
        {
            return string.Empty;
        }

        return $"<details class=\"tool-call\"><summary>🔧 {WebUtility.HtmlEncode(toolName)}({WebUtility.HtmlEncode(formattedArgs)})</summary>{Environment.NewLine}";
    }

    public string RenderToolResult(string toolName, string toolResult)
    {
        // Must mirror RenderToolCallStart's gating: signal tools never emitted an opening <details>,
        // so emitting the closing markup here too would produce mismatched/dangling HTML tags.
        if (_signalToolNames.Contains(toolName))
        {
            // "__phase:" sentinels are only ever produced by signal tools (see ToolCallDispatcher),
            // so this check is already fully covered by the _signalToolNames gate above. A non-signal
            // tool's genuine result must never be swallowed just because it happens to start with
            // "__phase:" - RenderToolCallStart already opened a <details> for it that needs closing.
            return string.Empty;
        }

        var encodedResult = WebUtility.HtmlEncode(Truncate(toolResult, MaxResultLength));
        return $"<div class=\"tool-result\">{localizer["ToolResult", encodedResult]}</div></details>{Environment.NewLine}";
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
