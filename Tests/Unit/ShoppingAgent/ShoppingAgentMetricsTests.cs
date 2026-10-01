using System.Diagnostics.Metrics;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingAgentMetricsTests
{
    [Test]
    public void Constructor_CreatesInstrumentsWithExpectedMetadata_WhenInvoked()
    {
        // Arrange
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));

        // Act
        var metrics = new ShoppingAgentMetrics(meterFactory);

        // Assert
        metrics.SessionsCreated.Name.Should().Be("shopping_agent.sessions.created");
        metrics.SessionsCreated.Description.Should().Be("Total number of shopping sessions created");

        metrics.ActiveSessions.Name.Should().Be("shopping_agent.sessions.active");
        metrics.ActiveSessions.Description.Should().Be("Number of active shopping sessions");

        metrics.MessagesProcessed.Name.Should().Be("shopping_agent.messages.processed");
        metrics.MessagesProcessed.Description.Should().Be("Total number of user messages processed");

        metrics.ToolCallsTotal.Name.Should().Be("shopping_agent.tool_calls.total");
        metrics.ToolCallsTotal.Description.Should().Be("Total number of tool calls executed");

        metrics.ToolCallsFailed.Name.Should().Be("shopping_agent.tool_calls.failed");
        metrics.ToolCallsFailed.Description.Should().Be("Total number of failed tool calls");

        metrics.LlmResponseTimeMs.Name.Should().Be("shopping_agent.llm.response_time_ms");
        metrics.LlmResponseTimeMs.Unit.Should().Be("ms");
        metrics.LlmResponseTimeMs.Description.Should().Be("LLM API response time in milliseconds");

        metrics.ToolExecutionTimeMs.Name.Should().Be("shopping_agent.tool.execution_time_ms");
        metrics.ToolExecutionTimeMs.Unit.Should().Be("ms");
        metrics.ToolExecutionTimeMs.Description.Should().Be("Tool execution time in milliseconds");

        metrics.MessageProcessingTimeMs.Name.Should().Be("shopping_agent.message.processing_time_ms");
        metrics.MessageProcessingTimeMs.Unit.Should().Be("ms");
        metrics.MessageProcessingTimeMs.Description.Should().Be("Total time to process a user message, including all LLM calls and tool executions in the turn");

        metrics.RetriesTotal.Name.Should().Be("shopping_agent.llm.retries_total");
        metrics.RetriesTotal.Description.Should().Be("Total number of LLM retries due to rate limiting");

        metrics.ModelFallbacksTotal.Name.Should().Be("shopping_agent.llm.model_fallbacks_total");
        metrics.ModelFallbacksTotal.Description.Should().Be("Total number of model fallback attempts");
    }
}
