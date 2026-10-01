using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Shared;

namespace Tests.Unit.ShopAndEat.Components;

[TestFixture]
[Category("Unit")]
public class NavMenuTests : BunitContext
{
    [SetUp]
    public void SetUp()
    {
        var localizer = Substitute.For<IStringLocalizer<NavMenu>>();
        localizer["ShoppingAssistant"].Returns(new LocalizedString("ShoppingAssistant", "Shopping Assistant"));
        Services.Add(new ServiceDescriptor(typeof(IStringLocalizer<NavMenu>), localizer));
    }

    [Test]
    public void Render_ShouldDisplayNavbar()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navbar = cut.Find(".navbar");

        // Assert
        navbar.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayBrandName()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var brand = cut.Find(".navbar-brand");

        // Assert
        brand.Should().NotBeNull();
        brand.TextContent.Should().Contain("ShopAndEat");
        brand.GetAttribute("href").Should().Be(string.Empty);
    }

    [Test]
    public void Render_ShouldDisplayNavbarToggler()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var toggler = cut.Find(".navbar-toggler");

        // Assert
        toggler.Should().NotBeNull();
        toggler.ClassList.Should().Contain("navbar-toggler");
    }

    [Test]
    public void Render_ShouldDisplayMealNavLink()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navLinks = cut.FindAll("a.nav-link");
        var mealLink = navLinks.FirstOrDefault(link => string.Equals(link.GetAttribute("href"), "meal", StringComparison.Ordinal));

        // Assert
        mealLink.Should().NotBeNull();
        mealLink!.TextContent.Should().Contain("Meal");
        mealLink.GetAttribute("data-enhance-nav").Should().Be("false");
    }

    [Test]
    public void Render_ShouldDisplayRecipeNavLink()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navLinks = cut.FindAll("a.nav-link");
        var recipeLink = navLinks.FirstOrDefault(link => string.Equals(link.GetAttribute("href"), "recipe", StringComparison.Ordinal));

        // Assert
        recipeLink.Should().NotBeNull();
        recipeLink!.TextContent.Should().Contain("Recipe");
        recipeLink.GetAttribute("data-enhance-nav").Should().Be("false");
    }

    [Test]
    public void Render_ShouldDisplayArticleNavLink()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navLinks = cut.FindAll("a.nav-link");
        var articleLink = navLinks.FirstOrDefault(link => string.Equals(link.GetAttribute("href"), "article", StringComparison.Ordinal));

        // Assert
        articleLink.Should().NotBeNull();
        articleLink!.TextContent.Should().Contain("Article");
        articleLink.GetAttribute("data-enhance-nav").Should().Be("false");
    }

    [Test]
    public void Render_ShouldDisplayShoppingAssistantNavLink()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navLinks = cut.FindAll("a.nav-link");
        var shoppingLink = navLinks.FirstOrDefault(link => string.Equals(link.GetAttribute("href"), "shopping", StringComparison.Ordinal));

        // Assert
        shoppingLink.Should().NotBeNull();
        shoppingLink!.TextContent.Should().Contain("Shopping Assistant");
        shoppingLink.GetAttribute("data-enhance-nav").Should().Be("false");
    }

    [Test]
    public void Render_ShouldInitiallyHaveNavMenuCollapsed()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain(@"class=""collapse");
    }

    [Test]
    public void Render_ClickToggler_ShouldExpandNavMenu()
    {
        // Arrange
        var cut = Render<NavMenu>();
        var toggler = cut.Find(".navbar-toggler");

        // Act
        toggler.Click();
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            markup.Should().NotContain(@"class=""collapse");
        });

        // Assert
        var markupAfter = cut.Markup;
        markupAfter.Should().NotContain(@"class=""collapse");
    }

    [Test]
    public void Render_ShouldHaveFourNavLinks()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navLinks = cut.FindAll("a.nav-link");

        // Assert
        navLinks.Should().HaveCount(4);
    }

    [Test]
    public void Render_NavLinksContainIcons()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var spans = cut.FindAll(".oi");

        // Assert
        spans.Should().NotBeEmpty();
        spans.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Test]
    public void Render_ShoppingLinkShouldHaveCartIcon()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var markup = cut.Markup;

        // Assert
        markup.Should().Contain("oi-cart");
    }

    [Test]
    public void Render_NavbarHasDarkClass()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        var navbar = cut.Find(".navbar");

        // Assert
        navbar.ClassList.Should().Contain("navbar-dark");
    }
}
