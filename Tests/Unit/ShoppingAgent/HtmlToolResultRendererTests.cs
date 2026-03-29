using FluentAssertions;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Resources;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class HtmlToolResultRendererTests
{
    private IStringLocalizer<Messages> _localizerMock;
    private HtmlToolResultRenderer _sut;

    [SetUp]
    public void SetUp()
    {
        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
        {
            var key = call.ArgAt<string>(0);
            var args = call.ArgAt<object[]>(1);
            var formatted = string.Equals(key, "ToolResult", StringComparison.Ordinal) && args.Length > 0
                ? $"Result: {args[0]}"
                : key;
            return new LocalizedString(key, formatted);
        });

        _sut = new HtmlToolResultRenderer(_localizerMock);
    }

    [Test]
    public void RenderToolGroupStart_ProducesCorrectHtml()
    {
        // Arrange
        var icon = "🔍";
        var label = "Product Search";

        // Act
        var result = _sut.RenderToolGroupStart(icon, label);

        // Assert
        result.Should().Contain("<details class=\"tool-group\">");
        result.Should().Contain("<summary>🔍 Product Search</summary>");
    }

    [Test]
    public void RenderToolCallStart_ProducesCorrectHtml()
    {
        // Arrange
        var toolName = "search_products";
        var formattedArgs = "search_term=milk";

        // Act
        var result = _sut.RenderToolCallStart(toolName, formattedArgs);

        // Assert
        result.Should().Contain("<details class=\"tool-call\">");
        result.Should().Contain("<summary>🔧 search_products(search_term=milk)</summary>");
    }

    [Test]
    public void RenderToolResult_ShortResult_DoesNotTruncate()
    {
        // Arrange
        var toolResult = "Short result";

        // Act
        var result = _sut.RenderToolResult(toolResult);

        // Assert
        result.Should().Contain("<div class=\"tool-result\">");
        result.Should().Contain("Result: Short result");
        result.Should().Contain("</details>");
    }

    [Test]
    public void RenderToolResult_LongResult_Truncates()
    {
        // Arrange
        var toolResult = new string('x', 300);

        // Act
        var result = _sut.RenderToolResult(toolResult);

        // Assert
        result.Should().Contain("...");
        result.Should().Contain("<div class=\"tool-result\">");
    }

    [Test]
    public void RenderToolGroupEnd_ProducesClosingTags()
    {
        // Arrange & Act
        var result = _sut.RenderToolGroupEnd();

        // Assert
        result.Should().Contain("</details>");
    }
}
