using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.Concrete;

public class LlmCommunicator(
    IStringLocalizer<Messages> localizer,
    ILogger<LlmCommunicator> logger,
    ShoppingAgentMetrics metrics,
    IOptions<LlmClientOptions> llmOptions) : ILlmCommunicator
{
    // LLM providers expose no structured error code for "tool calling unsupported" failures;
    // the message wording is vendor-specific, so matching it is inherently heuristic.
    private const string ToolKeyword = "tool";
    private const string NotSupportedKeyword = "not supported";
    private const string UnsupportedKeyword = "unsupported";
    private const string DoesNotSupportKeyword = "does not support";

    private bool _toolCallingSupported = true;

    public async Task<(ChatResponse? Response, string? ErrorMessage, string? FallbackMessage)> GetResponseAsync(
        IChatClient chatClient,
        IList<ChatMessage> conversationHistory,
        Func<IReadOnlyList<AITool>> getTools,
        CancellationToken cancellationToken = default)
    {
        var options = BuildCurrentOptions(getTools);
        using var llmTimeout = CreateLinkedTimeoutSource(cancellationToken);
        using var activity = StartLlmCallActivity(options);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await chatClient.GetResponseAsync(conversationHistory, options, llmTimeout.Token);
            return RecordSuccess(activity, sw, response);
        }
        catch (OperationCanceledException) when (llmTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            AgentLogMessages.LlmCallTimedOut(logger);
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            return (null, $"{Environment.NewLine}{localizer["LlmTimeout"]}", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller-initiated cancellation (e.g. the Stop button), as opposed to the LLM-timeout case
            // above. Must propagate as a real cancellation, not a fake "LlmError", so the UI layer
            // (Home.razor) can show "Processing stopped." instead of a misleading error message.
            throw;
        }
        catch (Exception ex) when (IsToolCallingNotSupportedError(ex))
        {
            return await GetLlmResponseWithFallbackAsync(chatClient, conversationHistory, llmTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            AgentLogMessages.LlmCallFailed(logger, ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return (null, $"{Environment.NewLine}{localizer["LlmError", ex.Message]}", null);
        }
    }

    private static bool IsToolCallingNotSupportedError(Exception ex)
    {
        var message = ex.Message;
        return message.Contains(ToolKeyword, StringComparison.OrdinalIgnoreCase)
            && (message.Contains(NotSupportedKeyword, StringComparison.OrdinalIgnoreCase)
                || message.Contains(UnsupportedKeyword, StringComparison.OrdinalIgnoreCase)
                || message.Contains(DoesNotSupportKeyword, StringComparison.OrdinalIgnoreCase));
    }

    private (ChatResponse? Response, string? ErrorMessage, string? FallbackMessage) RecordSuccess(Activity? activity, Stopwatch sw, ChatResponse response)
    {
        // Stryker disable once all: reading ElapsedMilliseconds immediately after the awaited call is observably identical whether the stopwatch is explicitly stopped first or not.
        sw.Stop();
        activity?.SetTag("llm.status", "success");
        metrics.LlmResponseTimeMs.Record(sw.ElapsedMilliseconds);
        return (response, null, null);
    }

    private CancellationTokenSource CreateLinkedTimeoutSource(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(llmOptions.Value.TimeoutSeconds));
        return cts;
    }

    private Activity? StartLlmCallActivity(ChatOptions options)
    {
        var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.LlmCall");
        activity?.SetTag("llm.model", llmOptions.Value.DefaultModel);

        // Stryker disable once all: BuildCurrentOptions normalizes zero tools to null, so Count is never 0 when Tools is non-null.
        activity?.SetTag("llm.tools_enabled", options.Tools?.Count > 0);
        return activity;
    }

    private ChatOptions BuildCurrentOptions(Func<IReadOnlyList<AITool>> getTools)
    {
        var tools = _toolCallingSupported ? getTools().ToList() : [];
        return new ChatOptions { Tools = tools.Count > 0 ? tools : null };
    }

    private async Task<(ChatResponse? Response, string? ErrorMessage, string? FallbackMessage)> GetLlmResponseWithFallbackAsync(
        IChatClient chatClient,
        IList<ChatMessage> conversationHistory,
        CancellationTokenSource llmTimeout,
        CancellationToken cancellationToken)
    {
        AgentLogMessages.ToolCallingFallback(logger);
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ToolFallback");
        _toolCallingSupported = false;
        var fallbackMessage = $"{Environment.NewLine}{localizer["ToolCallingFallback"]}{Environment.NewLine}";
        var fallbackOptions = new ChatOptions();

        try
        {
            var response = await chatClient.GetResponseAsync(conversationHistory, fallbackOptions, llmTimeout.Token);
            return (response, null, fallbackMessage);
        }
        catch (OperationCanceledException) when (llmTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            return (null, $"{Environment.NewLine}{localizer["LlmTimeout"]}", fallbackMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See the analogous catch in GetResponseAsync: caller-initiated cancellation must propagate.
            throw;
        }
        catch (Exception retryEx)
        {
            activity?.SetStatus(ActivityStatusCode.Error, retryEx.Message);
            return (null, $"{Environment.NewLine}{localizer["LlmError", retryEx.Message]}", fallbackMessage);
        }
    }
}