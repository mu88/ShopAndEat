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
        using var context = new InMemoryDbContext();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var newArticleGroupDto = new NewArticleGroupDto("Vegetables");

        testee.CreateArticleGroup(newArticleGroupDto);

        context.ArticleGroups.Should().Contain(articleGroup => articleGroup.Name == "Vegetables");
    }

    [Test]
    public void DeleteArticleGroup()
    {
        using var context = new InMemoryDbContext();
        var existingArticleGroup = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.SaveChanges();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));
        var deleteArticleGroupDto = new DeleteArticleGroupDto(existingArticleGroup.Entity.ArticleGroupId);

        testee.DeleteArticleGroup(deleteArticleGroupDto);

        context.ArticleGroups.Should().NotContain(articleGroup => articleGroup.Name == "Vegetables");
    }

    [Test]
    public void GetAllArticleGroups()
    {
        using var context = new InMemoryDbContext();
        context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.ArticleGroups.Add(new ArticleGroup("Dairy"));
        context.SaveChanges();
        var testee = new ArticleGroupService(new SimpleCrudHelper(context));

        var results = testee.GetAllArticleGroups();

        results.Should().Contain(articleGroup => articleGroup.Name == "Vegetables").And.Contain(articleGroup => articleGroup.Name == "Dairy");
    }
}
