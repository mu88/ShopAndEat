using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Options;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class LlmClientOptionsTests
{
    [Test]
    public void Defaults_WhenNotConfigured_PointsToMistralApiWithEmptyApiKey()
    {
        // Arrange
        var testee = new LlmClientOptions();

        // Assert
        testee.Endpoint.Should().Be("https://api.mistral.ai/v1");
        testee.DefaultModel.Should().Be("mistral-small-2506");
        testee.FallbackModel.Should().Be("mistral-medium-2508");
        testee.TimeoutSeconds.Should().Be(60);
        testee.RetryMaxAttempts.Should().Be(3);
        testee.RetryBaseDelayMs.Should().Be(1000);
        testee.ApiKey.Should().BeEmpty();
    }
}
