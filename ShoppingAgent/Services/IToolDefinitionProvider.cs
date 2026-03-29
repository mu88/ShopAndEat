using Microsoft.Extensions.AI;

namespace ShoppingAgent.Services;

public interface IToolDefinitionProvider
{
    IReadOnlyList<AITool> GetToolDefinitions(string shopName);
}
