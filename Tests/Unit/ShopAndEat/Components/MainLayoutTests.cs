using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Shared;

namespace Tests.Unit.ShopAndEat.Components;

[TestFixture]
[Category("Unit")]
public class MainLayoutTests : BunitContext
{
    [SetUp]
    public void SetUp()
    {
        var localizer = Substitute.For<IStringLocalizer<NavMenu>>();
        localizer["ShoppingAssistant"].Returns(new LocalizedString("ShoppingAssistant", "Shopping Assistant"));
        Services.Add(new ServiceDescriptor(typeof(IStringLocalizer<NavMenu>), localizer));
    }

    [Test]
    public void Render_HasExpectedStructuralMarkup()
    {
        // Arrange
        RenderFragment body = builder => builder.AddContent(0, "Page content");

        // Act
        var cut = Render<MainLayout>(parameters => parameters.Add(p => p.Body, body));

        // Assert
        cut.Find(".page").Should().NotBeNull();
        cut.Find(".sidebar").Should().NotBeNull();
        cut.Find(".main").Should().NotBeNull();
        cut.Find(".content").TextContent.Should().Contain("Page content");
    }

    [Test]
    public void MainLayout_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(MainLayout);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Shared");
    }
}
