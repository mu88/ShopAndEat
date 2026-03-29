using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace ShopAndEat.Logging;

[ExcludeFromCodeCoverage]
internal static partial class ControllerLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping session {SessionId} created", SkipEnabledCheck = true)]
    public static partial void SessionCreated(ILogger logger, int sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping session {SessionId} completed", SkipEnabledCheck = true)]
    public static partial void SessionCompleted(ILogger logger, int sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopping session {SessionId} deleted", SkipEnabledCheck = true)]
    public static partial void SessionDeleted(ILogger logger, int sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Preference upserted (scope={Scope}, key={Key})", SkipEnabledCheck = true)]
    public static partial void PreferenceUpserted(ILogger logger, string scope, string key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Preference deleted (scope={Scope}, key={Key})", SkipEnabledCheck = true)]
    public static partial void PreferenceDeleted(ILogger logger, string scope, string key);
}
