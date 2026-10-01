using DTO.IngredientList;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DTO;

[TestFixture]
[Category("Unit")]
public class IngredientListDtosTests
{
    [Test]
    public void IngredientItem_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new IngredientItem();

        testee.Text.Should().Be(string.Empty);
        testee.Article.Should().Be(string.Empty);
        testee.Unit.Should().Be(string.Empty);
    }
}
