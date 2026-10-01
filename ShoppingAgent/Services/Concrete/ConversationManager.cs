using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Logging;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using WorkflowPhase = ShoppingAgent.Models.WorkflowPhase;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Manages the LLM conversation loop by coordinating LLM calls and tool execution.
/// </summary>
public class ConversationManager(
    IToolCallDispatcher dispatcher,
    ILlmCommunicator llmCommunicator,
    IToolExecutionOrchestrator toolExecutionOrchestrator,
    ILogger<ConversationManager> logger,
    ShoppingAgentMetrics metrics,
    IOptions<AgentOptions> agentOptions,
    IOptions<LlmClientOptions> llmOptions) : IConversationManager
{
    public WorkflowPhase Phase => dispatcher.Phase;

    public void ResetWorkflow() => dispatcher.ResetWorkflow();

    public ConversationProcessingResult ProcessAsync(
        IReadOnlyList<ChatMessage> conversationHistory,
        IChatClient chatClient,
        Func<IReadOnlyList<AITool>> getTools,
        string shopKey,
        CancellationToken cancellationToken = default)
    {
        var workingHistory = conversationHistory.ToList();
        var newMessages = new List<ChatMessage>();

        return new ConversationProcessingResult(
            ProcessAsyncCore(workingHistory, conversationHistory.Count, newMessages, chatClient, getTools, shopKey, cancellationToken),
            newMessages);
    }

    private static Activity? StartProcessActivity(string shopKey)
    {
        var activity = ShoppingAgentDiagnostics.ActivitySource.StartActivity("ShoppingAgent.ProcessMessage");
        activity?.SetTag("agent.shop", shopKey);
        return activity;
    }

    private static void AppendNewMessages(
        List<ChatMessage> workingHistory,
        int originalMessageCount,
        ICollection<ChatMessage> newMessages)
    {
        for (var index = originalMessageCount; index < workingHistory.Count; index++)
        {
            newMessages.Add(workingHistory[index]);
        }
    }

    private async IAsyncEnumerable<string> ProcessAsyncCore(
        List<ChatMessage> workingHistory,
        int originalMessageCount,
        IList<ChatMessage> newMessages,
        IChatClient chatClient,
        Func<IReadOnlyList<AITool>> getTools,
        string shopKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AgentLogMessages.ProcessingUserMessage(logger, llmOptions.Value.DefaultModel);
        using var processActivity = StartProcessActivity(shopKey);
        var sw = Stopwatch.StartNew();
        var iteration = 0;
        var processingState = new ConversationProcessingState();

        try
        {
            for (iteration = 0; iteration < agentOptions.Value.MaxToolCallingIterations; iteration++)
            {
                await foreach (var chunk in RunIterationAsync(
                    workingHistory, chatClient, getTools, shopKey, processingState, processActivity, cancellationToken))
                {
                    yield return chunk;
                }

                if (processingState.ShouldBreak)
                {
                    break;
                }
            }
        }
        finally
        {
            // Stryker disable once all: reading ElapsedMilliseconds at method exit observes the same duration whether the stopwatch is explicitly stopped first or not.
            sw.Stop();
            AgentLogMessages.MessageProcessingComplete(logger, iteration, sw.ElapsedMilliseconds);
            metrics.MessageProcessingTimeMs.Record(sw.ElapsedMilliseconds);

            // Appending here (not after the try/finally) ensures that messages already produced
            // before a cancellation/exception - including tool calls that already ran with real
            // side effects - are still surfaced to the caller instead of being silently discarded.
            AppendNewMessages(workingHistory, originalMessageCount, newMessages);
        }
    }

    private async IAsyncEnumerable<string> RunIterationAsync(
        List<ChatMessage> workingHistory,
        IChatClient chatClient,
        Func<IReadOnlyList<AITool>> getTools,
        string shopKey,
        ConversationProcessingState processingState,
        Activity? processActivity,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (response, errorMessage, fallbackMessage) =
            await llmCommunicator.GetResponseAsync(chatClient, workingHistory, getTools, cancellationToken);
        if (fallbackMessage is not null)
        {
            yield return fallbackMessage;
        }

        if (errorMessage is not null)
        {
            yield return errorMessage;
            processActivity?.SetStatus(ActivityStatusCode.Error, errorMessage);
            processingState.ShouldBreak = true;
            yield break;
        }

        await foreach (var chunk in toolExecutionOrchestrator.ProcessResponseAsync(
            response!, workingHistory, shopKey, processingState, cancellationToken))
        {
            yield return chunk;
        }
    }
}
