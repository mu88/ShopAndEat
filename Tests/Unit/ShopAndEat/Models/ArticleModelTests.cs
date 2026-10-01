using FluentAssertions;
using NUnit.Framework;
using ShopAndEat.Models;

namespace Tests.Unit.ShopAndEat.Models;

[TestFixture]
[Category("Unit")]
public class ArticleModelTests
{
    [Test]
    public void Defaults_WhenNotConfigured_HasEmptyArticleAndArticleGroupNames()
    {
        // Arrange
        var testee = new ArticleModel();

        // Assert
        testee.ArticleName.Should().BeEmpty();
        testee.ArticleGroupName.Should().BeEmpty();
    }
}
