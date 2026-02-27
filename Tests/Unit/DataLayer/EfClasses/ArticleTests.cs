using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ArticleTests
{
    [Test]
    public void CreateArticle()
    {
        var name = "Salat";
        var ingredientGroup = new ArticleGroup("Gemüse");

        var testee = new Article { Name = name, ArticleGroup = ingredientGroup, IsInventory = true };

        testee.Name.Should().Be(name);
        testee.ArticleGroup.Should().Be(ingredientGroup);
        testee.IsInventory.Should().Be(true);
    }
}
