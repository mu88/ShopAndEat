using System.Diagnostics;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
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
public class ArticleServiceTests
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
    public async Task CreateArticleAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var newArticleDto = new NewArticleDto("Cheese", new ExistingArticleGroupDto(articleGroup.Entity.ArticleGroupId, articleGroup.Entity.Name), true);
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);

        // Act
        await testee.CreateArticleAsync(newArticleDto);

        // Assert
        context.Articles.Should().Contain(article => article.Name == "Cheese");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleService.CreateArticleAsync");
    }

    [Test]
    public async Task DeleteArticleAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var deleteArticleGroupDto = new DeleteArticleDto(new global::DataLayer.EfClasses.ArticleId(3));
        var articleActionMock = Substitute.For<IArticleAction>();
        articleActionMock.DeleteArticleAsync(deleteArticleGroupDto).Returns(Task.CompletedTask);
        var testee = CreateTestee(articleActionMock, context);

        // Act
        await testee.DeleteArticleAsync(deleteArticleGroupDto);

        // Assert
        await articleActionMock.Received(1).DeleteArticleAsync(deleteArticleGroupDto);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleService.DeleteArticleAsync");
    }

    [Test]
    public async Task DeleteArticleAsync_WithRealArticleAction_PersistsDeletion()
    {
        // Arrange — uses the real ArticleAction/ArticleDbAccess (not a mock), which only calls
        // context.Articles.Remove() without saving, to prove ArticleService's own SaveChangesAsync
        // is what actually persists the deletion. Reads back via a SECOND context instance on the
        // same shared in-memory DB name, since the first context's change tracker would otherwise
        // mask a missing SaveChangesAsync call.
        var dbName = Guid.NewGuid().ToString();
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var article = writeContext.Articles.Add(new ArticleBuilder().WithDefaults().Build());
            await writeContext.SaveChangesAsync();

            var articleAction = new ArticleAction(new ArticleDbAccess(writeContext));
            var testee = CreateTestee(articleAction, writeContext);
            var deleteArticleDto = new DeleteArticleDto(article.Entity.ArticleId);

            // Act
            await testee.DeleteArticleAsync(deleteArticleDto);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.Articles.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllArticlesAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var articleActionMock = Substitute.For<IArticleAction>();
        var articleGroup = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Diary");
        var cheese = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Cheese", articleGroup, true);
        var butter = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(2), "Butter", articleGroup, true);
        articleActionMock.GetAllArticlesAsync().Returns(Task.FromResult<IReadOnlyList<ExistingArticleDto>>(new[] { cheese, butter }));
        var testee = CreateTestee(articleActionMock, context);

        // Act
        var result = await testee.GetAllArticlesAsync();

        // Assert
        await articleActionMock.Received(1).GetAllArticlesAsync();
        result.Should().SatisfyRespectively(first => first.Name.Should().Be("Butter"), second => second.Name.Should().Be("Cheese"));
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleService.GetAllArticlesAsync");
    }

    [Test]
    public async Task UpdateArticleAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build()).Entity;
        var article = context.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var articleActionMock = Substitute.For<IArticleAction>();
        var testee = CreateTestee(articleActionMock, context);
        var existingArticleDto = new ExistingArticleDto(article.ArticleId,
            "Cheese Updated",
            new ExistingArticleGroupDto(articleGroup.ArticleGroupId, articleGroup.Name),
            true);

        // Act
        await testee.UpdateArticleAsync(existingArticleDto);

        // Assert
        context.Articles.Single(a => a.ArticleId == article.ArticleId).Name.Should().Be("Cheese Updated");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleService.UpdateArticleAsync");
    }

    [Test]
    public async Task UpdateArticleAsync_PersistsChangesAcrossContextInstances()
    {
        // Arrange — reads back via a SECOND context instance on the same shared in-memory DB name,
        // since the first context's change tracker reflects the in-place entity mutation regardless
        // of whether SaveChangesAsync actually ran.
        var dbName = Guid.NewGuid().ToString();
        int articleId;
        int articleGroupId;
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var articleGroup = writeContext.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build()).Entity;
            var article = writeContext.Articles.Add(new ArticleBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();
            articleId = article.ArticleId.Value;
            articleGroupId = articleGroup.ArticleGroupId.Value;

            var articleActionMock = Substitute.For<IArticleAction>();
            var testee = CreateTestee(articleActionMock, writeContext);
            var existingArticleDto = new ExistingArticleDto(
                article.ArticleId,
                "Cheese Updated",
                new ExistingArticleGroupDto(articleGroup.ArticleGroupId, articleGroup.Name),
                true);

            // Act
            await testee.UpdateArticleAsync(existingArticleDto);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.Articles.Single(a => a.ArticleId.Value == articleId).Name.Should().Be("Cheese Updated");
    }

    private static ArticleService CreateTestee(IArticleAction articleActionMock, EfCoreContext context)
        => new(articleActionMock, context, new SimpleCrudHelper(context));
}
