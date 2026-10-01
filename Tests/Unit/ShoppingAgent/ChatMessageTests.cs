using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Models;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ChatMessageTests
{
    [Test]
    public void Defaults_WhenNotConfigured_HasUserRoleAndEmptyContent()
    {
        // Arrange
        var testee = new ChatMessage();

        // Assert
        testee.Role.Should().Be("user");
        testee.Content.Should().BeEmpty();
    }
}
