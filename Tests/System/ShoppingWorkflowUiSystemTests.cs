using System.Globalization;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Tests.System;

[Category("System")]
public class ShoppingWorkflowUiSystemTests
{
    private IPage? _page;
    private CancellationTokenSource _cancellationTokenSource = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public async Task Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        _cancellationToken = _cancellationTokenSource.Token;

        // Create a new page for each test
        _page = await SystemTestsFixture.Browser.NewPageAsync();
    }

    [TearDown]
    public async Task Teardown()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }

        _cancellationTokenSource?.Dispose();
    }

    [Test]
    public async Task ShoppingWorkflow_ShouldCreateArticleRecipeMealAndShoppingList()
    {
        // Arrange: reference data (ArticleGroup/Unit/MealType/Store) is seeded once for all System
        // tests by SystemTestsFixture.OneTimeSetUp - see SeedReferenceDataAsync there.
        var articleName = $"SystemTestArticle{Guid.NewGuid()}";
        var recipeName = $"SystemTestRecipe{Guid.NewGuid()}";

        // Act & Assert: Execute full workflow
        await CreateArticleViaUiAsync(articleName);
        await VerifyArticleExistsViaUiAsync(articleName);

        await CreateRecipeViaUiAsync(recipeName, 2, 1, articleName);
        await VerifyRecipeExistsViaUiAsync(recipeName);

        var mealId = await PlanMealViaUiAsync(DateTime.Now, recipeName, 3, 1);
        await VerifyMealExistsViaUiAsync(recipeName, 3);

        await GenerateShoppingListViaUiAsync();
        await VerifyMealIsMarkedAsShoppedViaUiAsync(mealId);

        await ToggleMealHasBeenShoppedViaUiAsync(mealId);
        await VerifyMealIsNotMarkedAsShoppedViaUiAsync(mealId);
    }

    private async Task CreateArticleViaUiAsync(string articleName)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/article", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await _page.WaitForSelectorAsync("[data-testid='article-name-input']", new() { Timeout = 15000 });

        await _page.FillAsync("[data-testid='article-name-input']", articleName);
        await _page.SelectOptionAsync("[data-testid='article-group-select']", SystemTestsFixture.SeededArticleGroupName);
        await _page.ClickAsync("[data-testid='article-save-button']");

        await Task.Delay(500, _cancellationToken);
    }

    private async Task VerifyArticleExistsViaUiAsync(string articleName)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/article", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var articleText = await _page.ContentAsync();
        articleText.Should().Contain(articleName, "because the created article should be visible in the list");
    }

    private async Task CreateRecipeViaUiAsync(string recipeName, int days, int persons, string articleName)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/recipe", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await _page.WaitForSelectorAsync("[data-testid='recipe-name-input']", new() { Timeout = 15000 });

        await _page.FillAsync("[data-testid='recipe-name-input']", recipeName);
        await _page.FillAsync("[data-testid='recipe-days-input']", days.ToString(CultureInfo.InvariantCulture));
        await _page.FillAsync("[data-testid='recipe-persons-input']", persons.ToString(CultureInfo.InvariantCulture));

        await _page.FillAsync("[data-testid='ingredient-quantity-0']", 1.5.ToString(CultureInfo.InvariantCulture));
        await _page.SelectOptionAsync("[data-testid='ingredient-unit-0']", SystemTestsFixture.SeededUnitName);
        await _page.SelectOptionAsync("[data-testid='ingredient-article-0']", articleName);

        await _page.ClickAsync("[data-testid='recipe-save-button']");
        await Task.Delay(500, _cancellationToken);
    }

    private async Task VerifyRecipeExistsViaUiAsync(string recipeName)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/recipe", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var recipeText = await _page.ContentAsync();
        recipeText.Should().Contain(recipeName, "because the created recipe should be visible in the list");
    }

    private async Task<int> PlanMealViaUiAsync(DateTime mealDate, string recipeName, int persons, int days)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await _page.WaitForSelectorAsync("[data-testid='meal-recipe-select']", new() { Timeout = 15000 });

        await _page.SelectOptionAsync("[data-testid='meal-recipe-select']", recipeName);
        await Task.Delay(200, _cancellationToken);

        await _page.SelectOptionAsync("[data-testid='meal-type-select']", SystemTestsFixture.SeededMealTypeName);
        await _page.FillAsync("[data-testid='meal-date-input']", mealDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await _page.FillAsync("[data-testid='meal-persons-input']", persons.ToString(CultureInfo.InvariantCulture));
        await _page.FillAsync("[data-testid='meal-days-input']", days.ToString(CultureInfo.InvariantCulture));

        await _page.ClickAsync("[data-testid='meal-save-button']");
        await Task.Delay(500, _cancellationToken);

        var mealRows = await _page.QuerySelectorAllAsync("[data-testid^='meal-row-']");
        var mealRow = mealRows[^1];
        var mealRowTestId = await mealRow.GetAttributeAsync("data-testid");

        if (mealRowTestId != null && mealRowTestId.StartsWith("meal-row-", StringComparison.Ordinal))
        {
            var mealIdStr = mealRowTestId.Substring("meal-row-".Length);
            if (int.TryParse(mealIdStr, CultureInfo.InvariantCulture, out var mealId))
            {
                return mealId;
            }
        }

        throw new InvalidOperationException("Could not extract meal ID from created meal row");
    }

    private async Task VerifyMealExistsViaUiAsync(string recipeName, int persons)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var mealText = await _page.ContentAsync();
        mealText.Should().Contain(recipeName, "because the created meal should be visible in the plan");
        mealText.Should().Contain(persons.ToString(CultureInfo.InvariantCulture), "because the meal should show the number of persons");
    }

    private async Task GenerateShoppingListViaUiAsync()
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await _page.WaitForSelectorAsync("[data-testid='meal-store-select']", new() { Timeout = 5000 });

        await _page.SelectOptionAsync("[data-testid='meal-store-select']", SystemTestsFixture.SeededStoreName);
        await _page.ClickAsync("[data-testid='get-shopping-list-button']");

        await Task.Delay(1000, _cancellationToken);
    }

    private async Task VerifyMealIsMarkedAsShoppedViaUiAsync(int mealId)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var mealIdText = mealId.ToString(CultureInfo.InvariantCulture);
        await _page.WaitForSelectorAsync($"[data-testid='meal-has-been-shopped-{mealIdText}']", new() { Timeout = 5000 });

        var shopCheckElement = await _page.QuerySelectorAsync($"[data-testid='meal-has-been-shopped-{mealIdText}']");
        var shopText = await shopCheckElement!.TextContentAsync();

        shopText!.Should().Contain("True", "because the meal should be marked as shopped after generating the shopping list");
    }

    private async Task ToggleMealHasBeenShoppedViaUiAsync(int mealId)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var mealIdText = mealId.ToString(CultureInfo.InvariantCulture);
        await _page.WaitForSelectorAsync($"[data-testid='toggle-meal-button-{mealIdText}']", new() { Timeout = 5000 });

        await _page.ClickAsync($"[data-testid='toggle-meal-button-{mealIdText}']");
        await Task.Delay(500, _cancellationToken);
    }

    private async Task VerifyMealIsNotMarkedAsShoppedViaUiAsync(int mealId)
    {
        await _page!.GotoAsync($"{SystemTestsFixture.AppInternalAddress}/meal", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var mealIdText = mealId.ToString(CultureInfo.InvariantCulture);
        await _page.WaitForSelectorAsync($"[data-testid='meal-has-been-shopped-{mealIdText}']", new() { Timeout = 5000 });

        var shopCheckElement = await _page.QuerySelectorAsync($"[data-testid='meal-has-been-shopped-{mealIdText}']");
        var shopText = await shopCheckElement!.TextContentAsync();

        shopText!.Should().Contain("False", "because the meal should no longer be marked as shopped after toggling");
    }
}
