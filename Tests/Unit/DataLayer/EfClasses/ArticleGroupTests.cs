using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ArticleGroupTests
{
    [Test]
    public void CreateArticleGroup()
    {
        // Arrange
        var name = "Vegetables";

        // Act
        var testee = new ArticleGroup(name);

        // Assert
        testee.Name.Should().Be(name);
    }
}
