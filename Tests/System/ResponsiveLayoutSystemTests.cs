using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Tests.System;

[Category("System")]
public class ResponsiveLayoutSystemTests
{
    private const int RailWidth = 60;
    private const int SidebarWidth = 250;
    private const double Tolerance = 1;

    private IPage? _page;

    [TearDown]
    public async Task Teardown()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
    }

    [TestCase("meal", 390, 844)]
    [TestCase("meal", 844, 390)]
    [TestCase("meal", 768, 1024)]
    [TestCase("meal", 1440, 900)]
    [TestCase("shopping", 390, 844)]
    [TestCase("shopping", 844, 390)]
    [TestCase("shopping", 768, 1024)]
    [TestCase("shopping", 1440, 900)]
    public async Task Layout_ShouldNotOverflowAndNavigationShouldNotOverlapContent(string path, int width, int height)
    {
        // Arrange
        await OpenAsync(path, width, height);

        // Act
        var nav = await GetBoundsAsync(".sidebar");
        var content = await GetBoundsAsync(".content");
        var hasHorizontalOverflow = await _page!.EvaluateAsync<bool>("document.documentElement.scrollWidth > window.innerWidth");

        // Assert
        hasHorizontalOverflow.Should().BeFalse();
        if (ExpectsBottomBar(width, height))
        {
            nav.Bottom.Should().BeApproximately(height, Tolerance);
            nav.Width.Should().BeApproximately(width, Tolerance);
        }
        else
        {
            nav.Left.Should().BeApproximately(0, Tolerance);
            nav.Width.Should().BeApproximately(width >= 992 && height > 500 ? SidebarWidth : RailWidth, Tolerance);
            content.Left.Should().BeGreaterThanOrEqualTo(nav.Right - Tolerance);
        }
    }

    [TestCase(390, 844)]
    [TestCase(844, 390)]
    [TestCase(768, 1024)]
    [TestCase(1440, 900)]
    public async Task ShoppingChat_ShouldKeepInputVisibleAboveNavigation(int width, int height)
    {
        // Arrange
        await OpenAsync("shopping", width, height);
        await _page!.WaitForSelectorAsync(".chat-input-area");

        // Act
        var nav = await GetBoundsAsync(".sidebar");
        var chat = await GetBoundsAsync(".chat-container");
        var input = await GetBoundsAsync(".chat-input-area");

        // Assert
        var visibleBottom = ExpectsBottomBar(width, height) ? nav.Top : height;
        input.Bottom.Should().BeLessThanOrEqualTo(visibleBottom + Tolerance);
        chat.Bottom.Should().BeApproximately(visibleBottom, Tolerance);
    }

    private static bool ExpectsBottomBar(int width, int height) => width < 768 && !(width > height && height <= 500);

    private async Task OpenAsync(string path, int width, int height)
    {
        _page = await SystemTestsFixture.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await _page.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/{path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
    }

    private async Task<Bounds> GetBoundsAsync(string selector)
    {
        var rect = await _page!.EvaluateAsync<double[]>(
            "selector => { const r = document.querySelector(selector).getBoundingClientRect(); return [r.left, r.top, r.right, r.bottom]; }",
            selector);
        return new Bounds(rect[0], rect[1], rect[2], rect[3]);
    }

    private sealed record Bounds(double Left, double Top, double Right, double Bottom)
    {
        public double Width => Right - Left;
    }
}
