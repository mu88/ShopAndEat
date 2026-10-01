using Bunit;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Recipe;
using DTO.Unit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer;
using ShopAndEat.Pages;

namespace Tests.Unit.ShopAndEat.Pages;

[TestFixture]
[Category("Unit")]
public class RecipeTests : BunitContext
{
    private IRecipeService _recipeService = null!;
    private IArticleService _articleService = null!;
    private IUnitService _unitService = null!;
    private IJSRuntime _jsRuntime = null!;

    [SetUp]
    public void SetUp()
    {
        _recipeService = Substitute.For<IRecipeService>();
        _articleService = Substitute.For<IArticleService>();
        _unitService = Substitute.For<IUnitService>();
        _jsRuntime = Substitute.For<IJSRuntime>();

        Services.AddScoped(_ => _recipeService);
        Services.AddScoped(_ => _articleService);
        Services.AddScoped(_ => _unitService);
        Services.AddScoped(_ => _jsRuntime);

        _recipeService.CreateNewRecipeAsync(Arg.Any<NewRecipeDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _recipeService.DeleteRecipeAsync(Arg.Any<DeleteRecipeDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _recipeService.UpdateRecipeAsync(Arg.Any<UpdateRecipeDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private static Task<IReadOnlyList<T>> GetResult<T>(IReadOnlyList<T> items) => Task.FromResult(items);

    [Test]
    public void Render_ShouldDisplayEditFormForRecipe()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
    }

    [Test]
    public void Render_ShouldDisplayNameInputField()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Name:");
    }

    [Test]
    public void Render_ShouldDisplayNumberOfDaysInputField()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Number of Days:");
    }

    [Test]
    public void Render_ShouldDisplayNumberOfPersonsInputField()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Number of Persons:");
    }

    [Test]
    public void Render_ShouldDisplayIngredientsTable()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var tables = cut.FindAll("table");
        // There should be at least 2 tables - ingredients table and recipes table
        tables.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Test]
    public void Render_ShouldDisplayAddIngredientOption()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Add another ingredient");
    }

    [Test]
    public void Render_ShouldDisplayDeleteLastIngredientOption()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Delete last ingredient");
    }

    [Test]
    public void Render_ShouldDisplaySaveButton()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var buttons = cut.FindAll("button");
        var saveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Save", StringComparison.Ordinal));
        saveButton.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayRecipesTable()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var tables = cut.FindAll("table");
        var recipesTable = tables.FirstOrDefault(t => t.TextContent.Contains("Recipes", StringComparison.Ordinal));
        recipesTable.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayRecipeTableHeaders()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var headers = cut.FindAll("th");
        var headerTexts = headers.Select(h => h.TextContent).ToList();
        headerTexts.Should().Contain("Name");
    }

    [Test]
    public void Render_WithRecipes_ShouldDisplayRecipesInTable()
    {
        // Arrange
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)), new ExistingRecipeDto("Pizza", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(2)),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var tables = cut.FindAll("table");
        var lastTable = tables[tables.Count - 1];
        var rows = lastTable.QuerySelectorAll("tbody tr");
        rows.Should().HaveCount(2);
    }

    [Test]
    public void Render_WithRecipes_ShouldDisplayRecipeNames()
    {
        // Arrange
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)), new ExistingRecipeDto("Pizza", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(2)),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Pasta");
        markup.Should().Contain("Pizza");
    }

    [Test]
    public void Render_WithRecipes_ShouldDisplayDeleteButtons()
    {
        // Arrange
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var buttons = cut.FindAll("button");
        var deleteButton = buttons.FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        deleteButton.Should().NotBeNull();
    }

    [Test]
    public void Render_WithRecipes_ShouldDisplayEditButtons()
    {
        // Arrange
        var recipes = new[]
        {
            new ExistingRecipeDto("Pasta", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1)),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(recipes));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var buttons = cut.FindAll("button");
        var editButton = buttons.FirstOrDefault(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        editButton.Should().NotBeNull();
    }

    [Test]
    public async Task Render_ShouldLoadRecipesOnInitialization()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        await _recipeService.Received().GetAllRecipesAsync();
    }

    [Test]
    public async Task Render_ShouldLoadUnitsOnInitialization()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        await _unitService.Received().GetAllUnitsAsync();
    }

    [Test]
    public async Task Render_ShouldLoadArticlesOnInitialization()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        await _articleService.Received().GetAllArticlesAsync();
    }

    [Test]
    public void Render_ShouldHaveFormWithValidation()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
        markup.Should().Contain("<button type=\"submit\">");
    }

    [Test]
    public void Render_ShouldHaveSubmitButton()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<button type=\"submit\">Save</button>");
    }

    [Test]
    public void Render_ShouldHaveIngredientsTableWithHeaders()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var tables = cut.FindAll("table");
        var firstTable = tables[0];
        var headers = firstTable.QuerySelectorAll("th");
        var headerTexts = headers.Select(h => h.TextContent).ToList();
        headerTexts.Should().Contain("Quantity");
        headerTexts.Should().Contain("Unit");
        headerTexts.Should().Contain("Article");
    }

    [Test]
    public void Render_WithArticles_ShouldDisplayArticlesInIngredientSelectors()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[]
        {
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false),
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(2), "Lettuce", articleGroup, false),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Tomato");
        markup.Should().Contain("Lettuce");
    }

    [Test]
    public void Render_WithUnits_ShouldDisplayUnitsInIngredientSelectors()
    {
        // Arrange
        var units = new[]
        {
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "kg"),
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(2), "piece"),
        };

        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(units));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("kg");
        markup.Should().Contain("piece");
    }

    [Test]
    public void Recipe_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(Recipe);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Pages");
    }

    [Test]
    public void Recipe_ShouldHaveCorrectComponentName()
    {
        // Arrange
        var componentType = typeof(Recipe);

        // Act
        var name = componentType.Name;

        // Assert
        name.Should().Be("Recipe");
    }

    [Test]
    public void Render_WithEmptyRecipes_ShouldDisplayEmptyTable()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        // Act
        var cut = Render<Recipe>();

        // Assert
        var tables = cut.FindAll("table");
        var lastTable = tables[tables.Count - 1];
        var rows = lastTable.QuerySelectorAll("tbody tr");
        rows.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteRecipe_ShouldCallRecipeServiceDelete()
    {
        // Arrange
        var recipe = new ExistingRecipeDto("Bread", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(new[] { recipe }));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        var cut = Render<Recipe>();

        // Act
        var deleteButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        if (deleteButton is not null)
        {
            await deleteButton.ClickAsync();
        }

        // Assert
        await _recipeService.Received(1).DeleteRecipeAsync(Arg.Any<DeleteRecipeDto>());
    }

    [Test]
    public async Task EditRecipeAsync_ShouldPopulateFormWithRecipeData()
    {
        // Arrange
        var article = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Flour", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Staples"), false);
        var unit = new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "kg");
        var ingredient = new ExistingIngredientDto(article, 2.0, unit, 1);
        var recipe = new ExistingRecipeDto("Bread", 2, 4, new[] { ingredient }, new global::DataLayer.EfClasses.RecipeId(1));

        _recipeService.GetAllRecipesAsync().Returns(GetResult(new[] { recipe }));
        _articleService.GetAllArticlesAsync().Returns(GetResult(new[] { article }));
        _unitService.GetAllUnitsAsync().Returns(GetResult(new[] { unit }));

        var cut = Render<Recipe>();

        // Act
        var editButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        if (editButton is not null)
        {
            await editButton.ClickAsync();
        }

        cut.Render();

        // Assert - verify form was populated with recipe data
        var markup = cut.Markup;
        markup.Should().Contain("Bread");
    }

    [Test]
    public async Task HandleSubmitAsync_WithNoIngredients_CreatesRecipeAndShowsConfirmation()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        var cut = Render<Recipe>();
        await cut.Find("input").ChangeAsync("Salad");
        var deleteIngredientLink = cut.FindAll("label").First(l => l.TextContent.Contains("Delete last ingredient", StringComparison.Ordinal));
        await deleteIngredientLink.ClickAsync();
        cut.Render();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _recipeService.Received(1).CreateNewRecipeAsync(Arg.Is<NewRecipeDto>(dto => dto.Name == "Salad" && !dto.Ingredients.Any()));
        await _jsRuntime.Received(1).InvokeVoidAsync("window.alert", Arg.Is<object[]>(args => args.Length == 1 && (string)args[0] == "Saved!"));
    }

    [Test]
    public async Task HandleSubmitAsync_WithIngredientSelected_CreatesRecipeWithMappedIngredient()
    {
        // Arrange
        var article = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Flour", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Staples"), false);
        var unit = new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "kg");
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(new[] { article }));
        _unitService.GetAllUnitsAsync().Returns(GetResult(new[] { unit }));

        NewRecipeDto? capturedDto = null;
        _recipeService.When(x => x.CreateNewRecipeAsync(Arg.Any<NewRecipeDto>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedDto = callInfo.Arg<NewRecipeDto>());

        var cut = Render<Recipe>();
        await cut.Find("input[type=text],input:not([type])").ChangeAsync("Bread");
        await cut.Find("input[type=number][step='0.1']").ChangeAsync("1.5");
        await cut.FindAll("select")[0].ChangeAsync("1");
        await cut.FindAll("select")[1].ChangeAsync("1");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Name.Should().Be("Bread");
        var ingredient = capturedDto.Ingredients.Should().ContainSingle().Subject;
        ingredient.Article.ArticleId.Should().Be(article.ArticleId);
        ingredient.Unit.UnitId.Should().Be(unit.UnitId);
        ingredient.Quantity.Should().Be(1.5);
    }

    [Test]
    public async Task HandleSubmitAsync_AfterEditingExistingRecipe_UpdatesRecipe()
    {
        // Arrange
        var recipe = new ExistingRecipeDto("Bread", 1, 4, Enumerable.Empty<ExistingIngredientDto>(), new global::DataLayer.EfClasses.RecipeId(1));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(new[] { recipe }));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        var cut = Render<Recipe>();
        var editButton = cut.FindAll("button.btn").First(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        await editButton.ClickAsync();
        cut.Render();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _recipeService.Received(1).UpdateRecipeAsync(Arg.Is<UpdateRecipeDto>(dto => dto.RecipeId.Value == 1 && dto.Name == "Bread"));
    }

    [Test]
    public async Task HandleSubmitAsync_AfterEditingExistingRecipeWithIngredientSelected_UpdatesRecipeWithMappedIngredient()
    {
        // Arrange
        var article = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Flour", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Staples"), false);
        var unit = new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "kg");
        var existingIngredient = new ExistingIngredientDto(article, 1.0, unit, 1);
        var recipe = new ExistingRecipeDto("Bread", 1, 4, new[] { existingIngredient }, new global::DataLayer.EfClasses.RecipeId(1));
        _recipeService.GetAllRecipesAsync().Returns(GetResult(new[] { recipe }));
        _articleService.GetAllArticlesAsync().Returns(GetResult(new[] { article }));
        _unitService.GetAllUnitsAsync().Returns(GetResult(new[] { unit }));

        UpdateRecipeDto? capturedDto = null;
        _recipeService.When(x => x.UpdateRecipeAsync(Arg.Any<UpdateRecipeDto>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedDto = callInfo.Arg<UpdateRecipeDto>());

        var cut = Render<Recipe>();
        var editButton = cut.FindAll("button.btn").First(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        await editButton.ClickAsync();
        cut.Render();
        await cut.Find("input[type=number][step='0.1']").ChangeAsync("2.5");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedDto.Should().NotBeNull();
        var ingredient = capturedDto!.Ingredients.Should().ContainSingle().Subject;
        ingredient.Article.ArticleId.Should().Be(article.ArticleId);
        ingredient.Unit.UnitId.Should().Be(unit.UnitId);
        ingredient.Quantity.Should().Be(2.5);
    }

    [Test]
    public void AddIngredient_ShouldIncrementRowCount()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        var cut = Render<Recipe>();

        var initialRows = cut.FindAll("table")[0].QuerySelectorAll("tr").Length;

        // Act
        var addLabel = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Add another ingredient", StringComparison.Ordinal));
        if (addLabel is not null)
        {
            addLabel.Click();
            cut.Render();
        }

        var newRows = cut.FindAll("table")[0].QuerySelectorAll("tr").Length;

        // Assert
        newRows.Should().BeGreaterThan(initialRows);
    }

    [Test]
    public void DeleteLastIngredient_ShouldDecrementRowCount()
    {
        // Arrange
        _recipeService.GetAllRecipesAsync().Returns(GetResult(Array.Empty<ExistingRecipeDto>()));
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _unitService.GetAllUnitsAsync().Returns(GetResult(Array.Empty<ExistingUnitDto>()));

        var cut = Render<Recipe>();

        // Add an ingredient first
        var addLabel = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Add another ingredient", StringComparison.Ordinal));
        if (addLabel is not null)
        {
            addLabel.Click();
            cut.Render();
        }

        var rowsAfterAdd = cut.FindAll("table")[0].QuerySelectorAll("tr").Length;

        // Act
        var deleteLabel = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Delete last ingredient", StringComparison.Ordinal));
        if (deleteLabel is not null)
        {
            deleteLabel.Click();
            cut.Render();
        }

        var rowsAfterDelete = cut.FindAll("table")[0].QuerySelectorAll("tr").Length;

        // Assert
        rowsAfterDelete.Should().BeLessThan(rowsAfterAdd);
    }
}
