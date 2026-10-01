using Bunit;
using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Pages;

namespace Tests.Unit.ShopAndEat.Pages;

[TestFixture]
[Category("Unit")]
public class NotFoundTests : BunitContext
{
    [Test]
    public void Render_ShouldDisplayNotFoundHeading()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var heading = cut.Find("h1");

        // Assert
        heading.Should().NotBeNull();
        heading.TextContent.Should().Be("Not Found");
    }

    [Test]
    public void Render_ShouldDisplayNotFoundMessage()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var paragraph = cut.Find("p");

        // Assert
        paragraph.Should().NotBeNull();
        paragraph.TextContent.Should().Be("Sorry, there's nothing at this address.");
    }

    [Test]
    public void Render_ShouldHaveOnlyOneHeading()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var headings = cut.FindAll("h1");

        // Assert
        headings.Should().HaveCount(1);
    }

    [Test]
    public void Render_ShouldHaveOnlyOneParagraph()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var paragraphs = cut.FindAll("p");

        // Assert
        paragraphs.Should().HaveCount(1);
    }

    [Test]
    public void Render_ShouldContainApologeticMessage()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var paragraph = cut.Find("p");

        // Assert
        paragraph.TextContent.Should().Contain("Sorry");
        paragraph.TextContent.Should().Contain("nothing at this address");
    }

    [Test]
    public void Render_ShouldNotHaveErrorClasses()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var heading = cut.Find("h1");
        var content = cut.Markup;

        // Assert
        heading.ClassList.Should().BeEmpty();
        content.Should().NotContain("text-danger");
        content.Should().NotContain("error");
    }

    [Test]
    public void Render_ShouldRenderSimpleStaticContent()
    {
        // Arrange
        var cut = Render<NotFound>();

        // Act
        var componentMarkup = cut.Markup;

        // Assert
        componentMarkup.Should().NotBeNullOrEmpty();
        componentMarkup.Should().Contain("Not Found");
        componentMarkup.Should().Contain("Sorry");
    }
}
