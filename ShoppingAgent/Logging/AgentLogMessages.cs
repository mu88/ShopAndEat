using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace ShoppingAgent.Logging;

[ExcludeFromCodeCoverage]
internal static partial class AgentLogMessages
{
    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Agent initialized for shop {ShopKey}")]
    public static partial void AgentInitialized(ILogger logger, string shopKey);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Switching shop to {NewShopKey}")]
    public static partial void SwitchingShop(ILogger logger, string newShopKey);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Processing user message (model: {ModelName})")]
    public static partial void ProcessingUserMessage(ILogger logger, string modelName);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "LLM call timed out after 60 seconds")]
    public static partial void LlmCallTimedOut(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Tool calling not supported by model, switching to text-only mode")]
    public static partial void ToolCallingFallback(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "LLM call failed: {ErrorMessage}")]
    public static partial void LlmCallFailed(ILogger logger, string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing tool {ToolName}({Args})")]
    public static partial void ExecutingTool(ILogger logger, string toolName, string args);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Tool {ToolName} failed (failure #{FailCount}): {Error}")]
    public static partial void ToolCallFailed(ILogger logger, string toolName, int failCount, string error);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Tool {ToolName} failed {FailCount} times in a row, aborting iteration")]
    public static partial void ToolCallRepeatedFailure(ILogger logger, string toolName, int failCount);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Message processing complete after {Iterations} LLM iterations ({ElapsedMs}ms)")]
    public static partial void MessageProcessingComplete(ILogger logger, int iterations, long elapsedMs);
}
