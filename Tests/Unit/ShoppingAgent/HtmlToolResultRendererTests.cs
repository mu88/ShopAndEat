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
    private IStringLocalizer<Messages> _localizerMock = null!;
    private HtmlToolResultRenderer _sut = null!;

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
        var result = _sut.RenderToolResult("search_products", toolResult);

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
        var result = _sut.RenderToolResult("search_products", toolResult);

        // Assert
        result.Should().Contain("...");
        result.Should().Contain("<div class=\"tool-result\">");
    }

    [Test]
    public void RenderToolResult_EmptyResult_DoesNotTruncate()
    {
        // Arrange — exercises the string.IsNullOrEmpty short-circuit branch of Truncate's guard clause.
        var toolResult = string.Empty;

        // Act
        var result = _sut.RenderToolResult("search_products", toolResult);

        // Assert
        result.Should().Contain("<div class=\"tool-result\">");
        result.Should().NotContain("...");
    }

    [Test]
    public void RenderToolResult_ResultExactlyAtMaxLength_DoesNotTruncate()
    {
        // Arrange — boundary test: exactly MaxResultLength (200) chars must NOT be truncated,
        // isolating the "<=" from a "<" mutation on the length comparison.
        var toolResult = new string('x', 200);

        // Act
        var result = _sut.RenderToolResult("search_products", toolResult);

        // Assert
        result.Should().NotContain("...");
        result.Should().Contain($"Result: {toolResult}");
    }

    [Test]
    public void RenderToolGroupEnd_ProducesClosingTags()
    {
        // Arrange & Act
        var result = _sut.RenderToolGroupEnd();

        // Assert
        result.Should().Contain("</details>");
    }

    [Test]
    public void RenderToolResult_WithPhaseSentinel_FromSignalTool_ShouldReturnEmpty()
    {
        // Arrange & Act — "__phase:" sentinels are only ever produced by signal tools (confirm_cart/proceed_to_cart)
        var result = _sut.RenderToolResult("confirm_cart", "__phase:awaiting_confirmation__");

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolResult_WithFillingCartSentinel_FromSignalTool_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result = _sut.RenderToolResult("proceed_to_cart", "__phase:filling_cart__");

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolResult_FromNonSignalTool_ResultCoincidentallyStartingWithPhasePrefix_IsNotSwallowed()
    {
        // Arrange — a non-signal tool's genuine result must never be dropped just because its text
        // happens to start with "__phase:"; RenderToolCallStart already opened a <details> for it
        // that must be closed, otherwise the HTML is left dangling.
        var result = _sut.RenderToolResult("search_products", "__phase:awaiting_confirmation__ not actually a sentinel");

        // Assert
        result.Should().Contain("<div class=\"tool-result\">");
        result.Should().Contain("</details>");
        result.Should().Contain("__phase:awaiting_confirmation__");
    }

    [Test]
    public void RenderToolCallStart_WithConfirmCart_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result = _sut.RenderToolCallStart("confirm_cart", string.Empty);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolCallStart_WithProceedToCart_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result = _sut.RenderToolCallStart("proceed_to_cart", string.Empty);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolResult_WithConfirmCart_ShouldReturnEmpty()
    {
        // Arrange — RenderToolCallStart never opens a <details> tag for signal tools, so
        // RenderToolResult must not emit its closing markup either, or the HTML becomes mismatched.
        var result = _sut.RenderToolResult("confirm_cart", "some result");

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolResult_WithProceedToCart_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result = _sut.RenderToolResult("proceed_to_cart", "some result");

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void RenderToolResult_EncodesScriptPayload_FromScrapedContent()
    {
        // Arrange — simulates untrusted content coming back from a web-scraping tool call.
        var maliciousResult = "<script>alert(1)</script>";

        // Act
        var result = _sut.RenderToolResult("search_products", maliciousResult);

        // Assert — the payload must be neutralized as encoded text, never as executable markup.
        result.Should().NotContain("<script>alert(1)</script>");
        result.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Test]
    public void RenderToolResult_EncodesImgOnErrorPayload_FromScrapedContent()
    {
        // Arrange
        var maliciousResult = "<img src=x onerror=alert(1)>";

        // Act
        var result = _sut.RenderToolResult("search_products", maliciousResult);

        // Assert
        result.Should().NotContain("<img src=x onerror=alert(1)>");
        result.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
    }

    [Test]
    public void RenderToolCallStart_EncodesScriptPayload_InToolNameAndArgs()
    {
        // Arrange — the LLM picks the tool name/arguments, both are untrusted input.
        var toolName = "<script>alert('name')</script>";
        var formattedArgs = "<script>alert('args')</script>";

        // Act
        var result = _sut.RenderToolCallStart(toolName, formattedArgs);

        // Assert
        result.Should().NotContain("<script>alert('name')</script>");
        result.Should().NotContain("<script>alert('args')</script>");
        result.Should().Contain("&lt;script&gt;alert(&#39;name&#39;)&lt;/script&gt;");
        result.Should().Contain("&lt;script&gt;alert(&#39;args&#39;)&lt;/script&gt;");
    }

    [Test]
    public void RenderToolGroupStart_DoesNotAlterIconOrLabel_BecauseTheyAreDeveloperControlled()
    {
        // Arrange — groupIcon/groupLabel come from IToolCallDispatcher.GetToolGroup's fixed switch
        // expression, never from the LLM or scraped content, so no encoding should be applied to them.
        var groupIcon = "🔍";
        var groupLabel = "Product Search";

        // Act
        var result = _sut.RenderToolGroupStart(groupIcon, groupLabel);

        // Assert
        result.Should().Contain("<summary>🔍 Product Search</summary>");
    }
}
