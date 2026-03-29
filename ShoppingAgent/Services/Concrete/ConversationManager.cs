using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Manages the LLM conversation loop: sends messages, handles streaming, processes tool calls,
/// and tracks tool failures.
/// </summary>
public class ConversationManager(
    IToolCallDispatcher dispatcher,
    IToolResultRenderer renderer,
    IStringLocalizer<Messages> localizer,
    ILogger<ConversationManager> logger,
    ShoppingAgentMetrics metrics,
    IOptions<AgentOptions> agentOptions,
    IOptions<LlmClientOptions> llmOptions) : IConversationManager
{
    private bool _toolCallingSupported = true;

    public async IAsyncEnumerable<string> ProcessAsync(
        List<ChatMessage> conversationHistory,
        IChatClient chatClient,
        IReadOnlyList<AITool> tools,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveTools = _toolCallingSupported ? tools.ToList() : new List<AITool>();
        var options = new ChatOptions { Tools = effectiveTools.Count > 0 ? effectiveTools : null };

        AgentLogMessages.ProcessingUserMessage(logger, llmOptions.Value.DefaultModel);

        using var processActivity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ProcessMessage");
        processActivity?.SetTag("agent.shop", shopKey);

        var sw = Stopwatch.StartNew();
        var iteration = 0;
        var toolState = new ToolExecutionState();
        try
        {
            for (iteration = 0; iteration < agentOptions.Value.MaxToolCallingIterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (response, errorMessage, fallbackMessage, updatedOptions) =
                    await GetLlmResponseAsync(chatClient, conversationHistory, options, cancellationToken);
                options = updatedOptions;

                if (fallbackMessage != null)
                    yield return fallbackMessage;

                if (errorMessage != null)
                {
                    yield return errorMessage;
                    processActivity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                    break;
                }

                var toolCalls = response.Messages
                    .SelectMany(msg => msg.Contents.OfType<FunctionCallContent>())
                    .ToList();

                if (toolCalls.Count == 0)
                {
                    var textContent = string.Join(string.Empty, response.Messages
                        .SelectMany(msg => msg.Contents.OfType<TextContent>())
                        .Select(text => text.Text));

                    conversationHistory.Add(new ChatMessage(ChatRole.Assistant, textContent));
                    yield return textContent;
                    break;
                }

                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Messages.SelectMany(msg => msg.Contents).ToList()));

                var toolGroups = dispatcher.GroupConsecutiveToolCalls(toolCalls);

                await foreach (var chunk in ProcessToolGroupsAsync(toolGroups, toolState, conversationHistory, shopKey, cancellationToken))
                    yield return chunk;

                if (toolState.RepeatedFailureTool != null)
                {
                    yield return $"{Environment.NewLine}⚠️ {localizer["RepeatedToolFailure", toolState.RepeatedFailureTool]}{Environment.NewLine}";
                    break;
                }
            }
        }
        finally
        {
            sw.Stop();
            AgentLogMessages.MessageProcessingComplete(logger, iteration, sw.ElapsedMilliseconds);
            metrics.LlmResponseTimeMs.Record(sw.ElapsedMilliseconds);
        }
    }

    private async Task<(ChatResponse Response, string ErrorMessage, string FallbackMessage, ChatOptions UpdatedOptions)> GetLlmResponseAsync(
        IChatClient chatClient, List<ChatMessage> conversationHistory, ChatOptions options, CancellationToken cancellationToken)
    {
        using var llmTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        llmTimeout.CancelAfter(TimeSpan.FromSeconds(llmOptions.Value.TimeoutSeconds));
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.LlmCall");
        activity?.SetTag("llm.model", llmOptions.Value.DefaultModel);
        activity?.SetTag("llm.tools_enabled", options.Tools?.Count > 0);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await chatClient.GetResponseAsync(conversationHistory, options, llmTimeout.Token);
            sw.Stop();
            activity?.SetTag("llm.status", "success");
            metrics.LlmResponseTimeMs.Record(sw.ElapsedMilliseconds);
            return (response, null, null, options);
        }
        catch (OperationCanceledException) when (llmTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            AgentLogMessages.LlmCallTimedOut(logger);
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            return (null, $"{Environment.NewLine}{localizer["LlmTimeout"]}", null, options);
        }
        catch (Exception ex) when (IsToolCallingNotSupportedError(ex))
        {
            return await GetLlmResponseWithFallbackAsync(chatClient, conversationHistory, llmTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            AgentLogMessages.LlmCallFailed(logger, ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return (null, $"{Environment.NewLine}{localizer["LlmError", ex.Message]}", null, options);
        }
    }

    private async Task<(ChatResponse Response, string ErrorMessage, string FallbackMessage, ChatOptions UpdatedOptions)> GetLlmResponseWithFallbackAsync(
        IChatClient chatClient, List<ChatMessage> conversationHistory, CancellationTokenSource llmTimeout, CancellationToken cancellationToken)
    {
        AgentLogMessages.ToolCallingFallback(logger);
        using var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ToolFallback");
        _toolCallingSupported = false;
        var fallbackMessage = $"{Environment.NewLine}{localizer["ToolCallingFallback"]}{Environment.NewLine}";
        var fallbackOptions = new ChatOptions();
        try
        {
            var response = await chatClient.GetResponseAsync(conversationHistory, fallbackOptions, llmTimeout.Token);
            return (response, null, fallbackMessage, fallbackOptions);
        }
        catch (OperationCanceledException) when (llmTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "timeout");
            return (null, $"{Environment.NewLine}{localizer["LlmTimeout"]}", fallbackMessage, fallbackOptions);
        }
        catch (Exception retryEx)
        {
            activity?.SetStatus(ActivityStatusCode.Error, retryEx.Message);
            return (null, $"{Environment.NewLine}{localizer["LlmError", retryEx.Message]}", fallbackMessage, fallbackOptions);
        }
    }

    private async IAsyncEnumerable<string> ProcessToolGroupsAsync(
        List<(string Key, string Label, string Icon, List<FunctionCallContent> Tools)> toolGroups,
        ToolExecutionState state,
        List<ChatMessage> conversationHistory,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (groupKey, groupLabel, groupIcon, groupTools) in toolGroups)
        {
            yield return renderer.RenderToolGroupStart(groupIcon, groupLabel);

            using var groupActivity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ToolExecution");
            groupActivity?.SetTag("tool.group", groupKey);
            groupActivity?.SetTag("tool.count", groupTools.Count);

            foreach (var toolCall in groupTools)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var formattedArgs = dispatcher.FormatArgs(toolCall.Arguments);
                yield return renderer.RenderToolCallStart(toolCall.Name, formattedArgs);

                AgentLogMessages.ExecutingTool(logger, toolCall.Name, formattedArgs);
                metrics.ToolCallsTotal.Add(1, new KeyValuePair<string, object>("tool.name", toolCall.Name));

                var toolSw = Stopwatch.StartNew();
                var (toolResult, toolSuccess) = await dispatcher.DispatchAsync(toolCall, shopKey, cancellationToken);
                toolSw.Stop();
                metrics.ToolExecutionTimeMs.Record(toolSw.ElapsedMilliseconds, new KeyValuePair<string, object>("tool.name", toolCall.Name));

                TrackToolFailure(toolCall, toolSuccess, state);

                if (!toolSuccess)
                {
                    metrics.ToolCallsFailed.Add(1, new KeyValuePair<string, object>("tool.name", toolCall.Name));
                    AgentLogMessages.ToolCallFailed(logger, toolCall.Name,
                        state.FailureTracker.TryGetValue(toolCall.Name + "|" + dispatcher.FormatArgs(toolCall.Arguments), out var fc) ? fc : 1,
                        toolResult);
                }

                yield return renderer.RenderToolResult(toolResult);

                conversationHistory.Add(new ChatMessage(ChatRole.Tool,
                    [new FunctionResultContent(toolCall.CallId, toolResult)]));

                if (state.RepeatedFailureTool != null) break;
            }

            if (state.RepeatedFailureTool != null)
                groupActivity?.SetStatus(ActivityStatusCode.Error, $"Repeated failure: {state.RepeatedFailureTool}");

            yield return renderer.RenderToolGroupEnd();

            if (state.RepeatedFailureTool != null) break;
        }
    }

    private void TrackToolFailure(FunctionCallContent toolCall, bool toolSuccess, ToolExecutionState state)
    {
        var failureKey = toolCall.Name + "|" + dispatcher.FormatArgs(toolCall.Arguments);
        if (toolSuccess)
        {
            state.FailureTracker.Remove(failureKey);
        }
        else
        {
            state.FailureTracker.TryGetValue(failureKey, out var failCount);
            state.FailureTracker[failureKey] = ++failCount;
            if (failCount >= agentOptions.Value.ToolFailureThreshold)
            {
                AgentLogMessages.ToolCallRepeatedFailure(logger, toolCall.Name, failCount);
                state.RepeatedFailureTool = toolCall.Name;
            }
        }
    }

    private static bool IsToolCallingNotSupportedError(Exception ex)
    {
        var message = ex.Message.ToUpperInvariant();
        return message.Contains("TOOL", StringComparison.Ordinal)
            && (message.Contains("NOT SUPPORTED", StringComparison.Ordinal)
                || message.Contains("UNSUPPORTED", StringComparison.Ordinal)
                || message.Contains("DOES NOT SUPPORT", StringComparison.Ordinal));
    }

    private sealed class ToolExecutionState
    {
        public string RepeatedFailureTool { get; set; }
        public Dictionary<string, int> FailureTracker { get; } = new();
    }
}
