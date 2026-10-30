using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ShopAndEat.Components;
using ShopAndEat.Pages;
using ShopAndEat.Shared;

namespace Tests.Unit.ShopAndEat.Components;

[TestFixture]
[Category("Unit")]
public class RoutesTests
{
    [TestCase("/not-found", typeof(NotFound))]
    [TestCase("/shopping", typeof(global::ShoppingAgent.Pages.Home))]
    public async Task Routes_ResolvesPagesFromBothAssemblies(string path, Type expectedPage)
    {
        // Arrange
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.ComponentFactories.AddStub<MainLayout>(parameters => parameters.Get(layout => layout.Body)!);
        context.ComponentFactories.AddStub<NotFound>();
        context.ComponentFactories.AddStub<global::ShoppingAgent.Pages.Home>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(path);

        // Act
        var component = context.Render<Routes>();

        // Assert
        component.FindComponent<RouteView>().Instance.RouteData.PageType.Should().Be(expectedPage);
        component.FindComponent<RouteView>().Instance.DefaultLayout.Should().Be(typeof(MainLayout));
        component.FindComponent<FocusOnNavigate>().Instance.Selector.Should().Be("h1");
    }

    [Test]
    public async Task Routes_RendersNotFoundPage_WhenRouteDoesNotExist()
    {
        // Arrange
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.ComponentFactories.AddStub<MainLayout>(parameters => parameters.Get(layout => layout.Body)!);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/does-not-exist");

        // Act
        var component = context.Render<Routes>();

        // Assert
        component.Find("h1").TextContent.Should().Be("Not Found");
        component.Markup.Should().Contain("Sorry, there's nothing at this address.");
    }
}
