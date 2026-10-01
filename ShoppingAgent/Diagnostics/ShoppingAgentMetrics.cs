using System.Diagnostics.Metrics;

namespace ShoppingAgent.Diagnostics;

public sealed class ShoppingAgentMetrics
{
    public ShoppingAgentMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable IDISP001 // Meter is managed by IMeterFactory, not by this class
        var meter = meterFactory.Create(ShoppingAgentDiagnostics.MeterName);
#pragma warning restore IDISP001

        (SessionsCreated, ActiveSessions) = CreateSessionMetrics(meter);
        (MessagesProcessed, ToolCallsTotal, ToolCallsFailed) = CreateMessageAndToolMetrics(meter);
        (LlmResponseTimeMs, ToolExecutionTimeMs, MessageProcessingTimeMs) = CreateTimingMetrics(meter);
        (RetriesTotal, ModelFallbacksTotal) = CreateLlmRetryMetrics(meter);
    }

    public Counter<int> SessionsCreated { get; }

    public UpDownCounter<int> ActiveSessions { get; }

    public Counter<int> MessagesProcessed { get; }

    public Counter<int> ToolCallsTotal { get; }

    public Counter<int> ToolCallsFailed { get; }

    public Histogram<double> LlmResponseTimeMs { get; }

    public Histogram<double> ToolExecutionTimeMs { get; }

    /// <summary>
    /// Duration of an entire ConversationManager.ProcessAsync turn, which may span multiple LLM calls
    /// and tool-execution round-trips. Deliberately a separate histogram from
    /// <see cref="LlmResponseTimeMs"/> (which measures a single LLM API call, recorded in
    /// LlmCommunicator): mixing the two into one histogram would make its percentiles meaningless.
    /// </summary>
    public Histogram<double> MessageProcessingTimeMs { get; }

    public Counter<int> RetriesTotal { get; }

    public Counter<int> ModelFallbacksTotal { get; }

    private static (Counter<int> SessionsCreated, UpDownCounter<int> ActiveSessions) CreateSessionMetrics(Meter meter)
    {
        var sessionsCreated = meter.CreateCounter<int>(
            "shopping_agent.sessions.created",
            description: "Total number of shopping sessions created");
        var activeSessions = meter.CreateUpDownCounter<int>(
            "shopping_agent.sessions.active",
            description: "Number of active shopping sessions");
        return (sessionsCreated, activeSessions);
    }

    private static (Counter<int> MessagesProcessed, Counter<int> ToolCallsTotal, Counter<int> ToolCallsFailed) CreateMessageAndToolMetrics(Meter meter)
    {
        var messagesProcessed = meter.CreateCounter<int>(
            "shopping_agent.messages.processed",
            description: "Total number of user messages processed");
        var toolCallsTotal = meter.CreateCounter<int>(
            "shopping_agent.tool_calls.total",
            description: "Total number of tool calls executed");
        var toolCallsFailed = meter.CreateCounter<int>(
            "shopping_agent.tool_calls.failed",
            description: "Total number of failed tool calls");
        return (messagesProcessed, toolCallsTotal, toolCallsFailed);
    }

    private static (Histogram<double> LlmResponseTimeMs, Histogram<double> ToolExecutionTimeMs, Histogram<double> MessageProcessingTimeMs) CreateTimingMetrics(Meter meter)
    {
        var llmResponseTimeMs = meter.CreateHistogram<double>(
            "shopping_agent.llm.response_time_ms",
            unit: "ms",
            description: "LLM API response time in milliseconds");
        var toolExecutionTimeMs = meter.CreateHistogram<double>(
            "shopping_agent.tool.execution_time_ms",
            unit: "ms",
            description: "Tool execution time in milliseconds");
        var messageProcessingTimeMs = meter.CreateHistogram<double>(
            "shopping_agent.message.processing_time_ms",
            unit: "ms",
            description: "Total time to process a user message, including all LLM calls and tool executions in the turn");
        return (llmResponseTimeMs, toolExecutionTimeMs, messageProcessingTimeMs);
    }

    private static (Counter<int> RetriesTotal, Counter<int> ModelFallbacksTotal) CreateLlmRetryMetrics(Meter meter)
    {
        var retriesTotal = meter.CreateCounter<int>(
            "shopping_agent.llm.retries_total",
            description: "Total number of LLM retries due to rate limiting");
        var modelFallbacksTotal = meter.CreateCounter<int>(
            "shopping_agent.llm.model_fallbacks_total",
            description: "Total number of model fallback attempts");
        return (retriesTotal, modelFallbacksTotal);
    }
}
