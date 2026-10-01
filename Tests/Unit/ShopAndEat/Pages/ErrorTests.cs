using Bunit;
using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Pages;

namespace Tests.Unit.ShopAndEat.Pages;

[TestFixture]
[Category("Unit")]
public class ErrorTests : BunitContext
{
    [Test]
    public void Render_ShouldDisplayErrorHeading()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var heading = cut.Find("h1.text-danger");

        // Assert
        heading.Should().NotBeNull();
        heading.TextContent.Should().Contain("Error.");
    }

    [Test]
    public void Render_ShouldDisplayErrorMessage()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var h2 = cut.Find("h2.text-danger");

        // Assert
        h2.Should().NotBeNull();
        h2.TextContent.Should().Contain("An error occurred while processing your request.");
    }

    [Test]
    public void Render_ShouldDisplayDevelopmentModeHeading()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var h3 = cut.Find("h3");

        // Assert
        h3.Should().NotBeNull();
        h3.TextContent.Should().Be("Development Mode");
    }

    [Test]
    public void Render_ShouldContainDevelopmentEnvironmentInfo()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var content = cut.Markup;

        // Assert
        content.Should().Contain("Development");
        content.Should().Contain("error");
        content.Should().Contain("ASPNETCORE_ENVIRONMENT");
    }

    [Test]
    public void Render_ShouldMentionSwappingToDevelopmentEnvironment()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var content = cut.Markup;

        // Assert
        content.Should().Contain("Swapping to");
        content.Should().Contain("Development");
        content.Should().Contain("more detailed information");
    }

    [Test]
    public void Render_ShouldWarnAboutDeployment()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var content = cut.Markup;

        // Assert
        content.Should().Contain("deployed applications");
        content.Should().Contain("sensitive information");
    }

    [Test]
    public void Render_ShouldHaveCorrectTextContent()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var paragraphs = cut.FindAll("p");

        // Assert
        paragraphs.Should().NotBeEmpty();
        paragraphs.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void Render_ShouldMentionDevelopmentEnvironmentVariable()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var content = cut.Markup;

        // Assert
        content.Should().Contain("ASPNETCORE_ENVIRONMENT");
        content.Should().Contain("Development");
    }

    [Test]
    public void Render_ShouldHaveHeadingsWithDangerClass()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var h1 = cut.Find("h1");
        var h2 = cut.Find("h2");

        // Assert
        h1.ClassList.Should().Contain("text-danger");
        h2.ClassList.Should().Contain("text-danger");
    }

    [Test]
    public void Render_ShouldContainMultipleParagraphs()
    {
        // Arrange
        var cut = Render<Error>();

        // Act
        var paragraphs = cut.FindAll("p");

        // Assert
        paragraphs.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
