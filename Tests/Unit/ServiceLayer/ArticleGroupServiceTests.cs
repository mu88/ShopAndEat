using System.Diagnostics;
using DataLayer.EfClasses;
using DTO.ArticleGroup;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using Tests.Builders;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class ArticleGroupServiceTests
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

    [Test]
    public async Task CreateArticleGroupAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var newArticleGroupDto = new NewArticleGroupDto("Vegetables");

        // Act
        await testee.CreateArticleGroupAsync(newArticleGroupDto);

        // Assert
        context.ArticleGroups.Should().Contain(articleGroup => articleGroup.Name == "Vegetables");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleGroupService.CreateArticleGroupAsync");
    }

    [Test]
    public async Task DeleteArticleGroupAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existingArticleGroup = context.ArticleGroups.Add(new ArticleGroupBuilder().WithDefaults().Build());
        await context.SaveChangesAsync();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var deleteArticleGroupDto = new DeleteArticleGroupDto(existingArticleGroup.Entity.ArticleGroupId);

        // Act
        await testee.DeleteArticleGroupAsync(deleteArticleGroupDto);

        // Assert
        context.ArticleGroups.Should().NotContain(articleGroup => articleGroup.Name == existingArticleGroup.Entity.Name);
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleGroupService.DeleteArticleGroupAsync");
    }

    [Test]
    public async Task GetAllArticleGroupsAsync()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.ArticleGroups.Add(new ArticleGroup("Dairy"));
        await context.SaveChangesAsync();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));

        // Act
        var results = await testee.GetAllArticleGroupsAsync();

        // Assert
        results.Should().Contain(articleGroup => articleGroup.Name == "Vegetables").And.Contain(articleGroup => articleGroup.Name == "Dairy");
        _completedActivities.Should().ContainSingle(activity => activity.OperationName == "ArticleGroupService.GetAllArticleGroupsAsync");
    }
}
