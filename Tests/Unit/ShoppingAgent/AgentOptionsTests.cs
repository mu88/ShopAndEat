using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Options;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class AgentOptionsTests
{
    [Test]
    public void Defaults_WhenNotConfigured_EnablesRetryAndDisablesModelFallback()
    {
        // Arrange
        var testee = new AgentOptions();

        // Assert
        testee.RetryEnabled.Should().BeTrue();
        testee.ModelFallbackEnabled.Should().BeFalse();
        testee.MaxToolCallingIterations.Should().Be(50);
        testee.ToolFailureThreshold.Should().Be(3);
    }
}
