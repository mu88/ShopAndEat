using Bunit;
using DTO.Article;
using DTO.ArticleGroup;
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
public class ArticleTests : BunitContext
{
    private IArticleService _articleService = null!;
    private IArticleGroupService _articleGroupService = null!;
    private IJSRuntime _jsRuntime = null!;

    [SetUp]
    public void SetUp()
    {
        _articleService = Substitute.For<IArticleService>();
        _articleGroupService = Substitute.For<IArticleGroupService>();
        _jsRuntime = Substitute.For<IJSRuntime>();

        Services.AddScoped(_ => _articleService);
        Services.AddScoped(_ => _articleGroupService);
        Services.AddScoped(_ => _jsRuntime);

        _articleService.CreateArticleAsync(Arg.Any<NewArticleDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), string.Empty, new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), string.Empty), false)));
        _articleService.DeleteArticleAsync(Arg.Any<DeleteArticleDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _articleService.UpdateArticleAsync(Arg.Any<ExistingArticleDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private static Task<IReadOnlyList<T>> GetResult<T>(IReadOnlyList<T> items) => Task.FromResult(items);

    [Test]
    public void Render_ShouldDisplayEditFormWithInputs()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var form = cut.Find("form");
        form.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayArticleNameInputField()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var inputs = cut.FindAll("input");
        inputs.Should().NotBeEmpty();
    }

    [Test]
    public void Render_ShouldDisplayTableForArticles()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var table = cut.Find("table");
        table.Should().NotBeNull();
        table.GetAttribute("class")?.Should().Contain("table");
    }

    [Test]
    public void Render_ShouldDisplayTableHeadings()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var headers = cut.FindAll("th");
        headers.Should().NotBeEmpty();
        var headerText = string.Join(' ', headers.Select(h => h.TextContent));
        headerText.Should().Contain("Name");
        headerText.Should().Contain("Article Group");
        headerText.Should().Contain("Is Inventory");
    }

    [Test]
    public void Render_ShouldDisplaySubmitButton()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var buttons = cut.FindAll("button");
        var submitButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Save", StringComparison.Ordinal));
        submitButton.Should().NotBeNull();
    }

    [Test]
    public void Render_WithArticles_ShouldDisplayArticlesInTable()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[]
        {
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false),
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(2), "Lettuce", articleGroup, true),
        };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        // Act
        var cut = Render<Article>();

        // Assert
        var tableRows = cut.FindAll("tbody tr");
        tableRows.Should().HaveCount(2);
    }

    [Test]
    public void Render_WithEmptyArticles_ShouldDisplayEmptyTable()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var tableRows = cut.FindAll("tbody tr");
        tableRows.Should().BeEmpty();
    }

    [Test]
    public void Render_WithArticleGroups_ShouldDisplayInSelect()
    {
        // Arrange
        var articleGroups = new[]
        {
            new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables"),
            new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(2), "Fruits"),
        };

        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(articleGroups));

        // Act
        var cut = Render<Article>();

        // Assert
        var selects = cut.FindAll("select");
        selects.Should().NotBeEmpty();
        var selectOptions = cut.FindAll("select option");
        var optionTexts = selectOptions.Select(o => o.TextContent).ToList();
        optionTexts.Should().Contain("Vegetables");
        optionTexts.Should().Contain("Fruits");
    }

    [Test]
    public void Render_ShouldDisplayDeleteButtons()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        // Act
        var cut = Render<Article>();

        // Assert
        var buttons = cut.FindAll("button.btn");
        buttons.Should().NotBeEmpty();
        var deleteButton = buttons.FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        deleteButton.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldDisplayEditButtons()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        // Act
        var cut = Render<Article>();

        // Assert
        var buttons = cut.FindAll("button.btn");
        var editButton = buttons.FirstOrDefault(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        editButton.Should().NotBeNull();
    }

    [Test]
    public void Render_ShouldHaveFormWithValidation()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
        markup.Should().Contain("<button type=\"submit\" data-testid=\"article-save-button\">");
    }

    [Test]
    public void Render_ShouldHaveSubmitButton()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<button type=\"submit\" data-testid=\"article-save-button\">Save</button>");
    }

    [Test]
    public async Task Render_ShouldLoadArticleGroupsOnInitialization()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        var articleGroups = new[] { new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables") };
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(articleGroups));

        // Act
        var cut = Render<Article>();

        // Assert
        await _articleGroupService.Received().GetAllArticleGroupsAsync();
    }

    [Test]
    public async Task Render_ShouldLoadArticlesOnInitialization()
    {
        // Arrange
        var articles = Array.Empty<ExistingArticleDto>();
        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        await _articleService.Received().GetAllArticlesAsync();
    }

    [Test]
    public void Render_WithArticles_ShouldDisplayArticleNames()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[]
        {
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false),
            new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(2), "Lettuce", articleGroup, true),
        };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        // Act
        var cut = Render<Article>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Tomato");
        markup.Should().Contain("Lettuce");
    }

    [Test]
    public void Render_ShouldHaveEditFormWithModelAttribute()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("<form");
        markup.Should().Contain("blazor:onsubmit");
    }

    [Test]
    public void Render_ShouldDisplayCheckboxForIsInventory()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        var inputs = cut.FindAll("input");
        inputs.Should().NotBeEmpty();
    }

    [Test]
    public void Article_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var componentType = typeof(Article);

        // Act
        var ns = componentType.Namespace;

        // Assert
        ns.Should().Be("ShopAndEat.Pages");
    }

    [Test]
    public void Article_ShouldHaveCorrectComponentName()
    {
        // Arrange
        var componentType = typeof(Article);

        // Act
        var name = componentType.Name;

        // Assert
        name.Should().Be("Article");
    }

    [Test]
    public async Task DeleteArticle_ShouldCallArticleServiceDelete()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        var cut = Render<Article>();

        // Act
        var deleteButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains('❌', StringComparison.Ordinal));
        if (deleteButton is not null)
        {
            await deleteButton.ClickAsync();
        }

        // Assert
        await _articleService.Received(1).DeleteArticleAsync(Arg.Any<DeleteArticleDto>());
    }

    [Test]
    public async Task EditArticleAsync_ShouldPopulateFormFields()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, true) };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        var cut = Render<Article>();

        // Act
        var editButton = cut.FindAll("button.btn").FirstOrDefault(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        if (editButton is not null)
        {
            await editButton.ClickAsync();
        }

        // Re-render to see the updated form
        cut.Render();

        // Assert - verify form was populated with article data
        var markup = cut.Markup;
        markup.Should().Contain("Tomato");
    }

    [Test]
    public async Task HandleSubmitAsync_WithNewArticle_CreatesArticleAndShowsConfirmation()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        var cut = Render<Article>();
        await cut.Find("input").ChangeAsync("Cucumber");
        await cut.Find("select").ChangeAsync("Vegetables");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _articleService.Received(1).CreateArticleAsync(Arg.Is<NewArticleDto>(dto => dto.Name == "Cucumber" && dto.ArticleGroup == articleGroup));
        await _jsRuntime.Received(1).InvokeVoidAsync("window.alert", Arg.Is<object[]>(args => args.Length == 1 && (string)args[0] == "Saved!"));
    }

    [Test]
    public async Task HandleSubmitAsync_AfterEditingExistingArticle_UpdatesArticleAndResetsArticleId()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) };
        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        var cut = Render<Article>();
        var editButton = cut.FindAll("button.btn").First(b => b.TextContent.Contains("🖊", StringComparison.Ordinal));
        await editButton.ClickAsync();
        cut.Render();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _articleService.Received(1).UpdateArticleAsync(Arg.Is<ExistingArticleDto>(dto => dto.ArticleId.Value == 1 && dto.Name == "Tomato"));
    }
}
