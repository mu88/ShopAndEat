using BizDbAccess.Concrete;
using BizLogic.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Doubles;

namespace Tests.Integration.ServiceLayer;

[TestFixture]
[Category("Integration")]
public class ArticleServiceTests
{
    [Test]
    public void CreateArticle()
    {
        using var context = new InMemoryDbContext();
        var vegetables = context.ArticleGroups.Add(new ArticleGroup("Vegetables"));
        context.SaveChanges();
        var testee = CreateTestee(context);

        testee.CreateArticle(new NewArticleDto("Tomato",
            new ExistingArticleGroupDto(vegetables.Entity.ArticleGroupId, "Vegetables"),
            false));

        context.Articles.Should().Contain(x => x.Name == "Tomato");
    }

    private static ArticleService CreateTestee(EfCoreContext context)
    {
        var mapper = TestMapper.Create();
        var testee = new ArticleService(new ArticleAction(new ArticleDbAccess(context), mapper), context, mapper, new SimpleCrudHelper(context, mapper));
        return testee;
    }
}