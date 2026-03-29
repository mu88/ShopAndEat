using BizDbAccess.Concrete;
using BizLogic.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Integration.ServiceLayer;

[TestFixture]
[Category("Integration")]
public class ArticleServiceTests
{
    [Test]
    public void CreateArticle()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var vegetables = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.SaveChanges();
        var testee = CreateTestee(context);

        // Act
        testee.CreateArticle(new NewArticleDto("Tomato",
            new ExistingArticleGroupDto(vegetables.Entity.ArticleGroupId, "Vegetables"),
            false));

        // Assert
        context.Articles.Should().Contain(article => article.Name == "Tomato");
    }

    private static ArticleService CreateTestee(EfCoreContext context)
        => new(new ArticleAction(new ArticleDbAccess(context)), context, new SimpleCrudHelper(context));
}
