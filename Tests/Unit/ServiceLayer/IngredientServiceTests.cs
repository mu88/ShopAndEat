using System.Diagnostics;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class IngredientServiceTests
{
    private readonly List<Activity> _completedActivities = [];
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ServiceLayerDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

    private static DbContextOptions<EfCoreContext> CreateSharedDbOptions(string dbName)
        => new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(dbName).Options;

    [Test]
    public async Task CreateIngredientAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var newIngredientDto =
            new NewIngredientDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables"), false),
                2,
                new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "Piece"));
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        await testee.CreateIngredientAsync(newIngredientDto);

        // Assert
        ingredientActionMock.Received(1).CreateIngredient(newIngredientDto);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "IngredientService.CreateIngredientAsync");
    }

    [Test]
    public async Task CreateIngredientAsync_WithRealIngredientAction_PersistsCreation()
    {
        // Arrange — uses the real IngredientAction/IngredientDbAccess (not a mock), which only adds
        // to the context without saving, to prove IngredientService's own SaveChangesAsync is what
        // actually persists the creation. Reads back via a SECOND context instance on the same
        // shared in-memory DB name, since the first context's change tracker would otherwise mask a
        // missing SaveChangesAsync call.
        var dbName = Guid.NewGuid().ToString();
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var articleGroup = writeContext.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build()).Entity;
            var article = writeContext.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
            var unit = writeContext.Units.Add(new UnitBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();

            var newIngredientDto = new NewIngredientDto(
                new ExistingArticleDto(article.ArticleId, article.Name, new ExistingArticleGroupDto(articleGroup.ArticleGroupId, articleGroup.Name), false),
                2,
                new ExistingUnitDto(unit.UnitId, unit.Name));
            var ingredientAction = new IngredientAction(new IngredientDbAccess(writeContext));
            var testee = new IngredientService(ingredientAction, writeContext);

            // Act
            await testee.CreateIngredientAsync(newIngredientDto);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.Ingredients.Should().ContainSingle(ingredient => ingredient.Quantity == 2);
    }

    [Test]
    public async Task DeleteIngredientAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        ingredientActionMock.DeleteIngredientAsync(deleteIngredientGroupDto).Returns(Task.CompletedTask);
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        await testee.DeleteIngredientAsync(deleteIngredientGroupDto);

        // Assert
        await ingredientActionMock.Received(1).DeleteIngredientAsync(deleteIngredientGroupDto);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "IngredientService.DeleteIngredientAsync");
    }

    [Test]
    public async Task DeleteIngredientAsync_WithRealIngredientAction_PersistsDeletion()
    {
        // Arrange — uses the real IngredientAction/IngredientDbAccess (not a mock), which only
        // removes from the context without saving, to prove IngredientService's own
        // SaveChangesAsync is what actually persists the deletion.
        var dbName = Guid.NewGuid().ToString();
        int ingredientId;
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var ingredient = writeContext.Ingredients.Add(new IngredientBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();
            ingredientId = ingredient.IngredientId;

            var ingredientAction = new IngredientAction(new IngredientDbAccess(writeContext));
            var testee = new IngredientService(ingredientAction, writeContext);

            // Act
            await testee.DeleteIngredientAsync(new DeleteIngredientDto(ingredientId));
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.Ingredients.Should().NotContain(ingredient => ingredient.IngredientId == ingredientId);
    }

    [Test]
    public async Task GetAllIngredientsAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        ingredientActionMock.GetAllIngredientsAsync().Returns(Task.FromResult<IReadOnlyList<ExistingIngredientDto>>(Array.Empty<ExistingIngredientDto>()));
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        await testee.GetAllIngredientsAsync();

        // Assert
        await ingredientActionMock.Received(1).GetAllIngredientsAsync();
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "IngredientService.GetAllIngredientsAsync");
    }
}
