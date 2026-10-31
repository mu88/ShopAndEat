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
    public void Render_ShouldDisplayListForArticles()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        cut.Find("[data-testid='article-list']").Should().NotBeNull();
    }

    [Test]
    public void Render_WithArticle_ShouldDisplayNameGroupAndInventoryIcon()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(2), "Lettuce", articleGroup, true) };
        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        // Act
        var cut = Render<Article>();

        // Assert
        var row = cut.Find("[data-testid='article-row-2']");
        row.TextContent.Should().Contain("Lettuce").And.Contain("Vegetables");
        row.QuerySelector(".icon-shelving-unit").Should().NotBeNull();
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
        var submitButton = cut.Find("button[type='submit']");
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
        var tableRows = cut.FindAll("[data-testid^='article-row-']");
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
        var tableRows = cut.FindAll("[data-testid^='article-row-']");
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
        var deleteButton = buttons.FirstOrDefault(b => b.QuerySelector(".icon-trash") is not null);
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
        var editButton = buttons.FirstOrDefault(b => b.QuerySelector(".icon-pencil") is not null);
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
        cut.Find("form button[type='submit'][data-testid='article-save-button']").Should().NotBeNull();
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
        var saveButton = cut.Find("button[type='submit'][data-testid='article-save-button']");
        saveButton.GetAttribute("aria-label").Should().Be("Save");
        saveButton.QuerySelector(".icon-check").Should().NotBeNull();
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
        var deleteButton = cut.FindAll("button.btn").FirstOrDefault(b => b.QuerySelector(".icon-trash") is not null);
        if (deleteButton is not null)
        {
            await deleteButton.ClickAsync();
        }

        // Assert
        await _articleService.Received(1).DeleteArticleAsync(Arg.Any<DeleteArticleDto>());
    }

    [Test]
    public async Task EditArticle_ShouldPopulateFormFieldsInsideTheArticleRow()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        var articles = new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, true) };

        _articleService.GetAllArticlesAsync().Returns(GetResult(articles));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));

        var cut = Render<Article>();

        // Act
        await cut.Find("[data-testid='edit-article-button-1']").ClickAsync();

        // Assert
        var row = cut.Find("[data-testid='article-row-1']");
        row.QuerySelector("[data-testid='article-name-input']")!.GetAttribute("value").Should().Be("Tomato");
        row.QuerySelector("[data-testid='article-is-inventory-checkbox']")!.HasAttribute("checked").Should().BeTrue();
        cut.FindAll("[data-testid='article-name-input']").Should().ContainSingle();
    }

    [Test]
    public async Task EditArticle_ShouldNotScrollToTop()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        _articleService.GetAllArticlesAsync().Returns(GetResult(new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) }));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));
        var cut = Render<Article>();

        // Act
        await cut.Find("[data-testid='edit-article-button-1']").ClickAsync();

        // Assert
        _jsRuntime.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<string>().Should().NotContain("window.scrollTo");
    }

    [Test]
    public async Task CancelEdit_ShouldRestoreArticleRowAndNotUpdate()
    {
        // Arrange
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables");
        _articleService.GetAllArticlesAsync().Returns(GetResult(new[] { new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", articleGroup, false) }));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(new[] { articleGroup }));
        var cut = Render<Article>();
        await cut.Find("[data-testid='edit-article-button-1']").ClickAsync();

        // Act
        await cut.Find("[data-testid='article-cancel-button']").ClickAsync();

        // Assert
        cut.FindAll("[data-testid='article-cancel-button']").Should().BeEmpty();
        cut.Find("[data-testid='edit-article-button-1']").Should().NotBeNull();
        cut.Find("[data-testid='article-name-input']").GetAttribute("value").Should().BeNullOrEmpty();
        await _articleService.DidNotReceive().UpdateArticleAsync(Arg.Any<ExistingArticleDto>());
    }

    [Test]
    public void Render_WithoutEditing_ShouldNotShowCancelButton()
    {
        // Arrange
        _articleService.GetAllArticlesAsync().Returns(GetResult(Array.Empty<ExistingArticleDto>()));
        _articleGroupService.GetAllArticleGroupsAsync().Returns(GetResult(Array.Empty<ExistingArticleGroupDto>()));

        // Act
        var cut = Render<Article>();

        // Assert
        cut.FindAll("[data-testid='article-cancel-button']").Should().BeEmpty();
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
        var editButton = cut.FindAll("button.btn").First(b => b.QuerySelector(".icon-pencil") is not null);
        await editButton.ClickAsync();
        cut.Render();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await _articleService.Received(1).UpdateArticleAsync(Arg.Is<ExistingArticleDto>(dto => dto.ArticleId.Value == 1 && dto.Name == "Tomato"));
        cut.FindAll(".article-item-editing").Should().BeEmpty();
        cut.Find("[data-testid='article-name-input']").GetAttribute("value").Should().BeNullOrEmpty();
    }
}
