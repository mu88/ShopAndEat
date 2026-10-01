using DTO.OnlineArticleMapping;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DTO;

[TestFixture]
[Category("Unit")]
public class OnlineArticleMappingDtosTests
{
    [Test]
    public void ExistingOnlineArticleMappingDto_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new ExistingOnlineArticleMappingDto();

        testee.ArticleName.Should().Be(string.Empty);
        testee.StoreKey.Should().Be(string.Empty);
    }

    [Test]
    public void NewOnlineArticleMappingDto_DefaultsArticleNameToEmptyString()
    {
        new NewOnlineArticleMappingDto().ArticleName.Should().Be(string.Empty);
    }
}
