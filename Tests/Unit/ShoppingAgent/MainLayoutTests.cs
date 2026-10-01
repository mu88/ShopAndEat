using Bunit;
using FluentAssertions;
using NUnit.Framework;
using BunitContext = Bunit.BunitContext;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class MainLayoutTests
{
    [Test]
    public void MainLayout_RendersBodyContent()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<global::ShoppingAgent.Layout.MainLayout>(parameters => parameters
            .Add(p => p.Body, builder => builder.AddContent(0, "Hello from body")));

        // Assert
        cut.Markup.Should().Contain("Hello from body");
        cut.Find(".page").Should().NotBeNull();
    }
}
