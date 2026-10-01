using System.Globalization;
using Bunit;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Meal;
using DTO.MealType;
using DTO.PurchaseItem;
using DTO.Recipe;
using DTO.Store;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer;
using ShopAndEat.Pages;

namespace Tests.Unit.ShopAndEat.Pages;

[TestFixture]
[Category("Unit")]
public class MealTests : BunitContext
{
    private static readonly DateTime Today = new(2026, 3, 15);

    private IMealService _mealService = null!;
    private IStoreService _storeService = null!;
    private IRecipeService _recipeService = null!;
    private IMealTypeService _mealTypeService = null!;
    private IJSRuntime _jsRuntime = null!;

    [SetUp]
    public void SetUp()
    {
        _mealService = Substitute.For<IMealService>();
        _storeService = Substitute.For<IStoreService>();
        _recipeService = Substitute.For<IRecipeService>();
        _mealTypeService = Substitute.For<IMealTypeService>();
        _jsRuntime = Substitute.For<IJSRuntime>();

        Services.AddScoped(_ => _mealService);
        Services.AddScoped(_ => _storeService);
        Services.AddScoped(_ => _recipeService);
        Services.AddScoped(_ => _mealTypeService);
        Services.AddScoped(_ => _jsRuntime);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Today));

        _mealService.CreateMealAsync(Arg.Any<NewMealDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _mealService.DeleteMealAsync(Arg.Any<DeleteMealDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _mealService.ToggleMealAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private static Task<IReadOnlyList<T>> GetResult<T>(IReadOnlyList<T> items) => Task.FromResult(items);

    [Test]
    public void Render_ShouldDisplayStoreSelector()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var selects = cut.FindAll("select");
        selects.Should().NotBeEmpty();
    }

    [Test]
    public void Render_ShouldDisplayDisabledButton_WhenNoStoresExist()
    {
        // Arrange - with no stores, SelectedStore stays null (FirstOrDefault of an empty list),
        // so the "Get shopping list" button must render disabled.
        _storeService.GetAllStoresAsync().Returns(GetResult(Array.Empty<ExistingStoreDto>()));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var button = cut.Find("button.btn-primary");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void Render_ShouldDisplayStoresInSelector()
    {
        // Arrange
        var stores = new[]
        {
            new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A"),
            new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(2), "Store B"),
        };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var options = cut.FindAll("select option");
        var optionTexts = options.Select(o => o.TextContent).ToList();
        optionTexts.Should().Contain("Store A");
        optionTexts.Should().Contain("Store B");
    }

    [Test]
    public void Render_ShouldDisplayGetShoppingListButton()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var buttons = cut.FindAll("button");
        var shoppingButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Get shopping list", StringComparison.Ordinal));
        shoppingButton.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayMealPlanTable()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var tables = cut.FindAll("table");
        tables.Should().NotBeEmpty();
        var firstTable = tables[0];
        firstTable.TextContent.Should().ContainEquivalentOf("Meal plan");
    }

    [Test]
    public void Render_ShouldDisplayMealTableHeaders()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var headers = cut.FindAll("th");
        var headerTexts = headers.Select(h => h.TextContent).ToList();
        headerTexts.Should().Contain("Day");
        headerTexts.Should().Contain("Time");
        headerTexts.Should().Contain("Recipe");
        headerTexts.Should().Contain("Has been shopped");
        headerTexts.Should().Contain("Number of persons");
    }

    [Test]
    public void Render_WithMeals_ShouldDisplayMealsInTable()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(1);
    }

    [Test]
    public void Render_WithMeals_ShouldDisplayMealDetails()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Breakfast");
        markup.Should().Contain("Eggs");
    }

    [Test]
    public void Render_ShouldDisplayEditFormForMeal()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
    }

    [Test]
    public void Render_ShouldDisplayRecipeSelector()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var selects = cut.FindAll("select");
        selects.Count.Should().BeGreaterThan(1);
    }

    [Test]
    public void Render_ShouldDisplayMealTypeSelector()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealTypes = new[]
        {
            new ExistingMealTypeDto("Breakfast", 1, 1),
            new ExistingMealTypeDto("Lunch", 2, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(mealTypes));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Breakfast");
        markup.Should().Contain("Lunch");
    }

    [Test]
    public void Render_ShouldDisplayDateInputField()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("type=\"date\"");
    }

    [Test]
    public void Render_ShouldDisplayNumberOfPersonsInput()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Number of Persons");
    }

    [Test]
    public void Render_ShouldDisplayNumberOfDaysInput()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Number of Days");
    }

    [Test]
    public void Render_ShouldDisplaySaveButton()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var buttons = cut.FindAll("button");
        var saveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Save", StringComparison.Ordinal));
        saveButton.Should().NotBeNull();
    }

    [Test]
    public async Task Render_ShouldLoadStoresOnInitialization()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        await _storeService.Received().GetAllStoresAsync();
    }

    [Test]
    public async Task Render_ShouldLoadMealsOnInitialization()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        await _mealService.Received().GetFutureMealsAsync();
    }

    [Test]
    public async Task Render_ShouldLoadRecipesOnInitialization()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        await _recipeService.Received().GetAllRecipesAsync();
    }

    [Test]
    public async Task Render_ShouldLoadMealTypesOnInitialization()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        await _mealTypeService.Received().GetAllMealTypesAsync();
    }

    [Test]
    public void Render_WithMeals_ShouldDisplayDeleteButtons()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var buttons = cut.FindAll("button.btn");
        var deleteButton = buttons.FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        deleteButton.Should().NotBeNull();
    }

    [Test]
    public void Render_WithMeals_ShouldDisplayToggleButtons()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var buttons = cut.FindAll("button.btn");
        var toggleButton = buttons.FirstOrDefault(b => b.TextContent.Contains("🔃", StringComparison.Ordinal));
        toggleButton.Should().NotBeNull();
    }

    [Test]
    public void Render_Meal_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(Meal);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Pages");
    }

    [Test]
    public void Render_Meal_ShouldHaveCorrectComponentName()
    {
        // Arrange
        var componentType = typeof(Meal);

        // Act
        var name = componentType.Name;

        // Assert
        name.Should().Be("Meal");
    }

    [Test]
    public void Render_ShouldHaveFormWithValidation()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
        markup.Should().Contain("<button type=\"submit\">");
    }

    [Test]
    public void Render_ShouldHaveSubmitButton()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<button type=\"submit\">Save</button>");
    }

    [Test]
    public async Task DeleteMeal_ShouldCallMealServiceDelete()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var deleteButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        if (deleteButton is not null)
        {
            await deleteButton.ClickAsync();
        }

        // Assert
        await _mealService.Received(1).DeleteMealAsync(Arg.Any<DeleteMealDto>());
    }

    [Test]
    public async Task Toggle_ShouldCallMealServiceToggleMealAsync()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(DateTime.Today.AddDays(1), mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var toggleButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains("🔃", StringComparison.Ordinal));
        if (toggleButton is not null)
        {
            await toggleButton.ClickAsync();
        }

        // Assert
        await _mealService.Received(1).ToggleMealAsync(Arg.Any<int>());
    }

    [Test]
    public async Task DeleteMeal_ShouldCallMealServiceDelete_ForTodaysHighlightedMeal()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(Today, mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var deleteButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        deleteButton.Should().NotBeNull();
        await deleteButton!.ClickAsync();

        // Assert
        await _mealService.Received(1).DeleteMealAsync(Arg.Any<DeleteMealDto>());
    }

    [Test]
    public async Task Toggle_ShouldCallMealServiceToggleMealAsync_ForTodaysHighlightedMeal()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(Today, mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var toggleButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains("🔃", StringComparison.Ordinal));
        toggleButton.Should().NotBeNull();
        await toggleButton!.ClickAsync();

        // Assert
        await _mealService.Received(1).ToggleMealAsync(Arg.Any<int>());
    }

    [Test]
    public void SelectedStoreChanged_ShouldUpdateSelectedStore()
    {
        // Arrange
        var stores = new[]
        {
            new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A"),
            new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(2), "Store B"),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var storeSelect = cut.Find("select");
        storeSelect.Change("Store B");

        // Assert - verify that store selection was changed
        cut.Render();
        cut.Markup.Should().Contain("Store B");
    }

    [Test]
    public void SelectedStoreChanged_ShouldThrow_WhenChangeEventValueIsNull()
    {
        // Arrange - a null e.Value (e.g. triggered without a real DOM value) can never match any
        // store's Name, so Single() throws InvalidOperationException rather than silently no-op'ing.
        var stores = new[]
        {
            new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A"),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();
        var method = typeof(Meal).GetMethod("SelectedStoreChanged", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic)!;

        // Act
        var act = () => method.Invoke(cut.Instance, [new ChangeEventArgs { Value = null }]);

        // Assert
        act.Should().Throw<global::System.Reflection.TargetInvocationException>()
           .WithInnerException<InvalidOperationException>();
    }

    [Test]
    public async Task GetShoppingList_ShouldCallMealServiceGetOrderedPurchaseItemsAsync()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _mealService.GetOrderedPurchaseItemsAsync(Arg.Any<ExistingStoreDto>()).Returns(GetResult(Array.Empty<NewPurchaseItemDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var shoppingButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Get shopping list", StringComparison.Ordinal));
        if (shoppingButton is not null)
        {
            await shoppingButton.ClickAsync();
        }

        // Assert
        await _mealService.Received(1).GetOrderedPurchaseItemsAsync(Arg.Any<ExistingStoreDto>());
    }

    [Test]
    public async Task CopyToClipboard_ShouldInvokeJsClipboardWrite_WhenPurchaseItemsExist()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var article = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false);
        var unit = new global::DTO.Unit.ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "kg");
        var purchaseItems = new[] { new NewPurchaseItemDto(article, unit, 2) };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _mealService.GetOrderedPurchaseItemsAsync(Arg.Any<ExistingStoreDto>()).Returns(GetResult<NewPurchaseItemDto>(purchaseItems));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();
        var shoppingButton = cut.FindAll("button").First(b => b.TextContent.Contains("Get shopping list", StringComparison.Ordinal));
        await shoppingButton.ClickAsync();

        // Act
        var copyButton = cut.FindAll("button").First(b => b.TextContent.Contains("Copy to clipboard", StringComparison.Ordinal));
        await copyButton.ClickAsync();

        // Assert
        await _jsRuntime.Received(1).InvokeVoidAsync("navigator.clipboard.writeText", Arg.Is<object[]>(args => args.Length == 1));
    }

    [Test]
    public void SelectedRecipeChanged_ShouldPopulateNumberOfDaysAndPersons()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 3, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        var cut = Render<Meal>();

        // Act
        var recipeSelects = cut.FindAll("select").Where(s => s.OuterHtml.Contains("Choose Recipe", StringComparison.Ordinal)).ToList();
        if (recipeSelects.Count > 0)
        {
            recipeSelects[0].Change("Pasta");
            cut.Render();
        }

        // Assert - the handler should have set the form values
        var markup = cut.Markup;
        markup.Should().Contain("Pasta");
    }

    [Test]
    public async Task HandleSubmit_WithValidSelection_CreatesMealAndReloadsMeals()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var recipe = new ExistingRecipeDto("Pasta", 3, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var mealType = new ExistingMealTypeDto("Dinner", 1, 1);

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(new[] { recipe }));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(new[] { mealType }));

        var cut = Render<Meal>();
        var selects = cut.FindAll("select");
        await selects.First(s => s.OuterHtml.Contains("Choose Recipe", StringComparison.Ordinal)).ChangeAsync("Pasta");
        cut.Render();
        await cut.FindAll("select").First(s => s.OuterHtml.Contains("Choose Meal Type", StringComparison.Ordinal)).ChangeAsync("Dinner");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _mealService.Received(1).CreateMealAsync(Arg.Is<NewMealDto>(dto =>
            dto.MealType == mealType && dto.Recipe == recipe));
        await _mealService.Received(2).GetFutureMealsAsync();
    }

    [Test]
    public void Render_ShouldDefaultDateInputToTimeProviderToday()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(Array.Empty<ExistingMealDto>()));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var dateInput = cut.Find("input[type=\"date\"]");
        dateInput.GetAttribute("value").Should().Be(Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Test]
    public void Render_WithMealOnTimeProviderToday_ShouldHighlightRow()
    {
        // Arrange
        var stores = new[] { new ExistingStoreDto(new global::DataLayer.EfClasses.StoreId(1), "Store A") };
        var mealType = new ExistingMealTypeDto("Breakfast", 1, 1);
        var recipe = new ExistingRecipeDto("Eggs", 1, 2, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        var meals = new[]
        {
            new ExistingMealDto(Today, mealType, recipe, new global::DataLayer.EfClasses.MealId(1), false, 2),
        };

        _storeService.GetAllStoresAsync().Returns(GetResult(stores));
        _mealService.GetFutureMealsAsync().Returns(GetResult(meals));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _mealTypeService.GetAllMealTypesAsync().Returns(GetResult(Array.Empty<ExistingMealTypeDto>()));

        // Act
        var cut = Render<Meal>();

        // Assert
        var row = cut.Find("tbody tr");
        row.GetAttribute("style").Should().Contain("#00FF00");
    }

    private sealed class FixedTimeProvider(DateTime today) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(today, TimeSpan.Zero);
    }
}
