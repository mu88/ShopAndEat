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
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace ShoppingAgent.Services.Concrete;

public class ToolExecutionOrchestrator(
    IToolCallDispatcher dispatcher,
    IToolResultRenderer renderer,
    IToolResultCompressor compressor,
    IStringLocalizer<Messages> localizer,
    ILogger<ToolExecutionOrchestrator> logger,
    ShoppingAgentMetrics metrics,
    IOptions<AgentOptions> agentOptions) : IToolExecutionOrchestrator
{
    public async IAsyncEnumerable<string> ProcessResponseAsync(
        ChatResponse response,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        ConversationProcessingState state,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var toolCalls = GetToolCalls(response);
        if (toolCalls.Count == 0)
        {
            yield return HandleTextOnlyResponse(response, conversationHistory);
            state.ShouldBreak = true;
            yield break;
        }

        var inlineText = GetFinalTextContent(response);
        if (!string.IsNullOrEmpty(inlineText))
        {
            yield return inlineText;
        }

        conversationHistory.Add(BuildAssistantMessage(response));
        var toolGroups = dispatcher.GroupConsecutiveToolCalls(toolCalls);
        await foreach (var chunk in ProcessToolGroupsAsync(toolGroups, state, conversationHistory, shopKey, cancellationToken))
        {
            yield return chunk;
        }

        if (state.RepeatedFailureTool is not null)
        {
            yield return $"{Environment.NewLine}⚠️ {localizer["RepeatedToolFailure", state.RepeatedFailureTool]}{Environment.NewLine}";
            state.ShouldBreak = true;
            yield break;
        }

        UpdateBreakStateAfterToolExecution(state, inlineText);
    }

    private static string HandleTextOnlyResponse(ChatResponse response, IList<ChatMessage> conversationHistory)
    {
        var textContent = GetFinalTextContent(response);
        conversationHistory.Add(new ChatMessage(ChatRole.Assistant, textContent));
        return textContent;
    }

    private static List<FunctionCallContent> GetToolCalls(ChatResponse response)
        => response.Messages.SelectMany(message => message.Contents.OfType<FunctionCallContent>()).ToList();

    private static string GetFinalTextContent(ChatResponse response)
        => string.Join(string.Empty, response.Messages.SelectMany(message => message.Contents.OfType<TextContent>()).Select(text => text.Text));

    private static ChatMessage BuildAssistantMessage(ChatResponse response)
        => new(ChatRole.Assistant, response.Messages.SelectMany(message => message.Contents).ToList());

    private void UpdateBreakStateAfterToolExecution(ConversationProcessingState state, string inlineText)
    {
        if (!dispatcher.ShouldBreakAfterToolExecution)
        {
            return;
        }

        var silentClarification = dispatcher.Phase == WorkflowPhase.AwaitingClarification
                                  && string.IsNullOrEmpty(inlineText);
        if (!silentClarification)
        {
            state.ShouldBreak = true;
        }
    }

    private async IAsyncEnumerable<string> ProcessToolGroupsAsync(
        IReadOnlyList<(string Key, string Label, string Icon, IReadOnlyList<FunctionCallContent> Tools)> toolGroups,
        ConversationProcessingState state,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var group in toolGroups)
        {
            await foreach (var chunk in ProcessToolGroupAsync(group, state, conversationHistory, shopKey, cancellationToken))
            {
                yield return chunk;
            }

            if (state.RepeatedFailureTool is not null)
            {
                break;
            }
        }
    }

    private async IAsyncEnumerable<string> ProcessToolGroupAsync(
        (string Key, string Label, string Icon, IReadOnlyList<FunctionCallContent> Tools) group,
        ConversationProcessingState state,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return renderer.RenderToolGroupStart(group.Icon, group.Label);

        using var groupActivity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ToolExecution");
        groupActivity?.SetTag("tool.group", group.Key);
        groupActivity?.SetTag("tool.count", group.Tools.Count);

        await foreach (var chunk in ExecuteToolCallsAsync(group.Tools, state, conversationHistory, shopKey, cancellationToken))
        {
            yield return chunk;
        }

        if (state.RepeatedFailureTool is not null)
        {
            groupActivity?.SetStatus(ActivityStatusCode.Error, $"Repeated failure: {state.RepeatedFailureTool}");
        }

        yield return renderer.RenderToolGroupEnd();
    }

    private async IAsyncEnumerable<string> ExecuteToolCallsAsync(
        IReadOnlyList<FunctionCallContent> toolCalls,
        ConversationProcessingState state,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var toolCall in toolCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var chunk in ExecuteSingleToolCallAsync(toolCall, state, conversationHistory, shopKey, cancellationToken))
            {
                yield return chunk;
            }

            if (state.RepeatedFailureTool is not null)
            {
                break;
            }
        }
    }

    private async IAsyncEnumerable<string> ExecuteSingleToolCallAsync(
        FunctionCallContent toolCall,
        ConversationProcessingState state,
        IList<ChatMessage> conversationHistory,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var formattedArgs = dispatcher.FormatArgs(toolCall.Arguments);
        yield return renderer.RenderToolCallStart(toolCall.Name, formattedArgs);

        AgentLogMessages.ExecutingTool(logger, toolCall.Name, formattedArgs);
        metrics.ToolCallsTotal.Add(1, new KeyValuePair<string, object?>("tool.name", toolCall.Name));

        var toolSw = Stopwatch.StartNew();
        var (toolResult, toolSuccess) = await dispatcher.DispatchAsync(toolCall, shopKey, cancellationToken);

        // Stryker disable once all: the metric is recorded immediately after dispatch returns, so explicitly stopping the stopwatch first does not change the observed duration.
        toolSw.Stop();
        metrics.ToolExecutionTimeMs.Record(toolSw.ElapsedMilliseconds, new KeyValuePair<string, object?>("tool.name", toolCall.Name));

        var failureCount = TrackToolFailure(toolCall, toolSuccess, state);

        if (!toolSuccess)
        {
            metrics.ToolCallsFailed.Add(1, new KeyValuePair<string, object?>("tool.name", toolCall.Name));
            AgentLogMessages.ToolCallFailed(logger, toolCall.Name, failureCount, toolResult);
        }

        yield return renderer.RenderToolResult(toolCall.Name, toolResult);

        var compressedResult = compressor.Compress(toolCall.Name, toolResult);
        conversationHistory.Add(new ChatMessage(ChatRole.Tool,
            [new FunctionResultContent(toolCall.CallId, compressedResult)]));
    }

    /// <summary>
    /// Updates the failure tracker for the given tool call and returns the current failure count
    /// (0 when the call succeeded, so callers never need to re-query the dictionary they just updated).
    /// </summary>
    private int TrackToolFailure(FunctionCallContent toolCall, bool toolSuccess, ConversationProcessingState state)
    {
        var failureKey = toolCall.Name + "|" + dispatcher.FormatArgs(toolCall.Arguments);
        if (toolSuccess)
        {
            state.FailureTracker.Remove(failureKey);
            return 0;
        }

        state.FailureTracker.TryGetValue(failureKey, out var failCount);
        state.FailureTracker[failureKey] = ++failCount;
        if (failCount >= agentOptions.Value.ToolFailureThreshold)
        {
            AgentLogMessages.ToolCallRepeatedFailure(logger, toolCall.Name, failCount);
            state.RepeatedFailureTool = toolCall.Name;
        }

        return failCount;
    }
}
