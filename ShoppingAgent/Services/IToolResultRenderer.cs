namespace ShoppingAgent.Services;

public interface IToolResultRenderer
{
    string RenderToolGroupStart(string groupIcon, string groupLabel);
    string RenderToolCallStart(string toolName, string formattedArgs);
    string RenderToolResult(string toolName, string toolResult);
    string RenderToolGroupEnd();
}
