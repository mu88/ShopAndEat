using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class OnlineArticleMappingTests
{
    [Test]
    public void CreateOnlineArticleMapping()
    {
        // Arrange
        var articleName = "Tomato";
        var storeKey = "coop";
        var storeProductCode = "12345";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var testee = new OnlineArticleMapping(articleName, storeKey, storeProductCode, createdAt);

        // Assert
        testee.ArticleName.Should().Be(articleName);
        testee.StoreKey.Should().Be(storeKey);
        testee.StoreProductCode.Should().Be(storeProductCode);
        testee.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    public void DefaultConstructor_SetsStringPropertiesToEmptyString()
    {
        // Act — EF materializes instances via the protected parameterless constructor.
        var testee = (OnlineArticleMapping)Activator.CreateInstance(typeof(OnlineArticleMapping), nonPublic: true)!;

        // Assert
        testee.ArticleName.Should().Be(string.Empty);
        testee.StoreKey.Should().Be(string.Empty);
        testee.StoreProductCode.Should().Be(string.Empty);
        testee.StoreProductName.Should().Be(string.Empty);
    }

    [Test]
    public void RecordMatch_UpdatesAllMatchRelatedFieldsTogether()
    {
        // Arrange
        var testee = new OnlineArticleMapping("Tomato", "coop", "12345", DateTimeOffset.UtcNow);
        var matchedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        testee.RecordMatch("Bio Tomato 500g", 2.95m, 87.5f, MatchMethod.UserChosen, 6, matchedAt);

        // Assert
        testee.StoreProductName.Should().Be("Bio Tomato 500g");
        testee.StoreProductPrice.Should().Be(2.95m);
        testee.Confidence.Should().Be(87.5f);
        testee.MatchMethod.Should().Be(MatchMethod.UserChosen);
        testee.QuantityPerUnit.Should().Be(6);
        testee.LastUsedAt.Should().Be(matchedAt);
    }

    [Test]
    public void RecordFeedback_WithoutArgument_IncrementsFeedbackCountByOne()
    {
        // Arrange
        var testee = new OnlineArticleMapping("Tomato", "coop", "12345", DateTimeOffset.UtcNow);

        // Act
        testee.RecordFeedback();
        testee.RecordFeedback();

        // Assert
        testee.FeedbackCount.Should().Be(2);
    }

    [Test]
    public void RecordFeedback_WithExplicitIncrement_IncreasesFeedbackCountMonotonically()
    {
        // Arrange
        var testee = new OnlineArticleMapping("Tomato", "coop", "12345", DateTimeOffset.UtcNow);

        // Act
        testee.RecordFeedback(5);

        // Assert
        testee.FeedbackCount.Should().Be(5);
    }

    [Test]
    public void RecordFeedback_WithZeroIncrement_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var testee = new OnlineArticleMapping("Tomato", "coop", "12345", DateTimeOffset.UtcNow);

        // Act
        var act = () => testee.RecordFeedback(0);

        // Assert — proves FeedbackCount can no longer be driven negative or reset arbitrarily like a public setter would allow.
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("increment").WithMessage("Feedback increment must be positive.*");
    }

    [Test]
    public void RecordFeedback_WithNegativeIncrement_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var testee = new OnlineArticleMapping("Tomato", "coop", "12345", DateTimeOffset.UtcNow);

        // Act
        var act = () => testee.RecordFeedback(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("increment");
    }
}
