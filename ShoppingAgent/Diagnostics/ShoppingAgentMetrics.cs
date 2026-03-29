using System.Diagnostics.Metrics;

namespace ShoppingAgent.Diagnostics;

public sealed class ShoppingAgentMetrics
{
    public Counter<int> SessionsCreated { get; }
    public UpDownCounter<int> ActiveSessions { get; }
    public Counter<int> MessagesProcessed { get; }
    public Counter<int> ToolCallsTotal { get; }
    public Counter<int> ToolCallsFailed { get; }
    public Histogram<double> LlmResponseTimeMs { get; }
    public Histogram<double> ToolExecutionTimeMs { get; }

    public ShoppingAgentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ShoppingAgentDiagnostics.MeterName);
        SessionsCreated = meter.CreateCounter<int>(
            "shopping_agent.sessions.created",
            description: "Total number of shopping sessions created");
        ActiveSessions = meter.CreateUpDownCounter<int>(
            "shopping_agent.sessions.active",
            description: "Number of active shopping sessions");
        MessagesProcessed = meter.CreateCounter<int>(
            "shopping_agent.messages.processed",
            description: "Total number of user messages processed");
        ToolCallsTotal = meter.CreateCounter<int>(
            "shopping_agent.tool_calls.total",
            description: "Total number of tool calls executed");
        ToolCallsFailed = meter.CreateCounter<int>(
            "shopping_agent.tool_calls.failed",
            description: "Total number of failed tool calls");
        LlmResponseTimeMs = meter.CreateHistogram<double>(
            "shopping_agent.llm.response_time_ms",
            unit: "ms",
            description: "LLM API response time in milliseconds");
        ToolExecutionTimeMs = meter.CreateHistogram<double>(
            "shopping_agent.tool.execution_time_ms",
            unit: "ms",
            description: "Tool execution time in milliseconds");
    }
}
