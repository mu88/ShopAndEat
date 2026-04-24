using Microsoft.Extensions.AI;
using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IToolDefinitionProvider
{
    IReadOnlyList<AITool> GetToolDefinitions(string shopName, WorkflowPhase phase);
}
