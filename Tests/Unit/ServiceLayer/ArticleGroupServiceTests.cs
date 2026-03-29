using DataLayer.EfClasses;
using DTO.ArticleGroup;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class ArticleGroupServiceTests
{
    [Test]
    public void CreateArticleGroup()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var newArticleGroupDto = new NewArticleGroupDto("Vegetables");

        // Act
        testee.CreateArticleGroup(newArticleGroupDto);

        // Assert
        context.ArticleGroups.Should().Contain(articleGroup => articleGroup.Name == "Vegetables");
    }

    [Test]
    public void DeleteArticleGroup()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var existingArticleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.SaveChanges();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var deleteArticleGroupDto = new DeleteArticleGroupDto(existingArticleGroup.Entity.ArticleGroupId);

        // Act
        testee.DeleteArticleGroup(deleteArticleGroupDto);

        // Assert
        context.ArticleGroups.Should().NotContain(articleGroup => articleGroup.Name == "Vegetables");
    }

    [Test]
    public void GetAllArticleGroups()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.ArticleGroups.Add(new ArticleGroup("Dairy"));
        context.SaveChanges();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));

        // Act
        var results = testee.GetAllArticleGroups();

        // Assert
        results.Should().Contain(articleGroup => articleGroup.Name == "Vegetables").And.Contain(articleGroup => articleGroup.Name == "Dairy");
    }
}
