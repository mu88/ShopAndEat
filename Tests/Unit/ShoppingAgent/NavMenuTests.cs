using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using BunitContext = Bunit.BunitContext;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class NavMenuTests
{
    private BunitContext _ctx;
    private IStringLocalizer<global::ShopAndEat.Shared.NavMenu> _localizerMock;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _localizerMock = Substitute.For<IStringLocalizer<global::ShopAndEat.Shared.NavMenu>>();
        _localizerMock["ShoppingAssistant"].Returns(new LocalizedString("ShoppingAssistant", "Shopping Assistant"));
        _ctx.Services.AddSingleton(_localizerMock);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx?.Dispose();
    }

    [Test]
    public void NavMenu_RendersShoppingAssistantLink()
    {
        // Act
        var cut = _ctx.Render<global::ShopAndEat.Shared.NavMenu>();

        // Assert
        cut.Markup.Should().Contain("Shopping Assistant");
    }

    [Test]
    public void NavMenu_HasCorrectShoppingHref()
    {
        // Act
        var cut = _ctx.Render<global::ShopAndEat.Shared.NavMenu>();

        // Assert
        var shoppingLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(a => a.TextContent.Contains("Shopping Assistant", StringComparison.Ordinal));
        shoppingLink.Should().NotBeNull();
        shoppingLink!.GetAttribute("href").Should().Be("shopping");
    }
}
