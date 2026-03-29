using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace ShoppingAgent.Logging;

[ExcludeFromCodeCoverage]
internal static partial class ServiceLogMessages
{
    // SessionService
    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to get sessions: {ErrorMessage}")]
    public static partial void GetSessionsFailed(ILogger logger, string errorMessage);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to get ingredient list: {ErrorMessage}")]
    public static partial void GetIngredientListFailed(ILogger logger, string errorMessage);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to get units: {ErrorMessage}")]
    public static partial void GetUnitsFailed(ILogger logger, string errorMessage);

    // PreferencesService
    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to get preferences (storeKey={StoreKey}): {ErrorMessage}")]
    public static partial void GetPreferencesFailed(ILogger logger, string storeKey, string errorMessage);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to get preferences for article {ArticleName}: {ErrorMessage}")]
    public static partial void GetPreferencesForArticleFailed(ILogger logger, string articleName, string errorMessage);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Failed to delete preference (scope={Scope}, key={Key}): {ErrorMessage}")]
    public static partial void DeletePreferenceFailed(ILogger logger, string scope, string key, string errorMessage);

    // MistralChatClientProvider
    [LoggerMessage(Level = LogLevel.Debug, Message = "API key changed, cached Mistral client invalidated")]
    public static partial void ApiKeyInvalidated(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Creating new Mistral chat client for model {ModelName}")]
    public static partial void CreatingChatClient(ILogger logger, string modelName);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Mistral API key validation failed: {ErrorMessage}")]
    public static partial void ApiKeyValidationFailed(ILogger logger, string errorMessage);

    // ExtensionBridge
    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Extension bridge initialized")]
    public static partial void ExtensionBridgeInitialized(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Chrome extension connected")]
    public static partial void ExtensionConnected(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Information, Message = "Chrome extension disconnected")]
    public static partial void ExtensionDisconnected(ILogger logger);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Extension not connected, cannot execute tool {ToolName}")]
    public static partial void ExtensionNotConnected(ILogger logger, string toolName);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Warning, Message = "Tool call {ToolName} timed out (callId={CallId})")]
    public static partial void ToolCallTimedOut(ILogger logger, string toolName, string callId);

    [LoggerMessage(SkipEnabledCheck = true, Level = LogLevel.Error, Message = "Failed to parse tool result from extension: {ErrorMessage}")]
    public static partial void ToolResultParseFailed(ILogger logger, string errorMessage);

    // CoopToolExecutor
    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching Coop for '{SearchTerm}'")]
    public static partial void CoopSearching(ILogger logger, string searchTerm);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Coop search for '{SearchTerm}' returned {ResultCount} result(s)")]
    public static partial void CoopSearchComplete(ILogger logger, string searchTerm, int resultCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting Coop product details for {ProductUrl}")]
    public static partial void CoopGettingProductDetails(ILogger logger, string productUrl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Adding to Coop cart: {ProductUrl} (qty={Quantity})")]
    public static partial void CoopAddingToCart(ILogger logger, string productUrl, int quantity);
}
