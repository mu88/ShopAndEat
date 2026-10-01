using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class ArticleMappingRepositoryTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));

    [Test]
    public async Task SaveOrUpdateMappingAsync_WhenNoMatchingMappingExists_CreatesNewMapping()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new ArticleMappingRepository(context, _timeProvider);
        var mapping = new OnlineArticleMapping("Tomate", "coop", "123", _timeProvider.GetUtcNow());
        mapping.RecordMatch("Bio Tomate", 2.5m, 90, MatchMethod.UserChosen, 6, _timeProvider.GetUtcNow());

        // Act
        await testee.SaveOrUpdateMappingAsync(mapping);

        // Assert
        context.OnlineArticleMappings.Should().ContainSingle(saved =>
            saved.ArticleName == "Tomate" && saved.StoreKey == "coop" && saved.StoreProductCode == "123" &&
            saved.StoreProductName == "Bio Tomate" && saved.StoreProductPrice == 2.5m && saved.Confidence == 90 &&
            saved.MatchMethod == MatchMethod.UserChosen && saved.QuantityPerUnit == 6 && saved.LastUsedAt == _timeProvider.GetUtcNow() &&
            saved.FeedbackCount == 0);
    }

    [Test]
    public async Task SaveOrUpdateMappingAsync_WhenStoreKeyAndProductCodeMatchButArticleNameDiffers_CreatesNewMappingInsteadOfUpdating()
    {
        // Arrange — proves ArticleName is also required to match (not just StoreKey+StoreProductCode),
        // i.e. that the first two conditions are combined with && rather than ||.
        await using var context = new InMemoryDbContext();
        var existing = new OnlineArticleMapping("Tomate", "coop", "123", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        existing.RecordFeedback(5);
        context.OnlineArticleMappings.Add(existing);
        await context.SaveChangesAsync();
        var testee = new ArticleMappingRepository(context, _timeProvider);
        var newMapping = new OnlineArticleMapping("Kartoffel", "coop", "123", _timeProvider.GetUtcNow());

        // Act
        await testee.SaveOrUpdateMappingAsync(newMapping);

        // Assert
        context.OnlineArticleMappings.Should().HaveCount(2);
        context.OnlineArticleMappings.Should().Contain(mapping => mapping.OnlineArticleMappingId == existing.OnlineArticleMappingId && mapping.FeedbackCount == 5);
        context.OnlineArticleMappings.Should().Contain(mapping => mapping.ArticleName == "Kartoffel" && mapping.FeedbackCount == 0);
    }

    [Test]
    public async Task SaveOrUpdateMappingAsync_WhenMatchingMappingExists_UpdatesItAndIncrementsFeedbackCount()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existing = new OnlineArticleMapping("Tomate", "coop", "123", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        existing.RecordMatch("Alte Tomate", 0m, 50, MatchMethod.FuzzyMatch, null, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        existing.RecordFeedback(2);
        context.OnlineArticleMappings.Add(existing);
        await context.SaveChangesAsync();
        var testee = new ArticleMappingRepository(context, _timeProvider);
        var updatedMapping = new OnlineArticleMapping("Tomate", "coop", "123", _timeProvider.GetUtcNow());
        updatedMapping.RecordMatch("Neue Bio Tomate", 3.1m, 95, MatchMethod.FuzzyMatch, 4, _timeProvider.GetUtcNow());

        // Act
        await testee.SaveOrUpdateMappingAsync(updatedMapping);

        // Assert
        context.OnlineArticleMappings.Should().ContainSingle(saved =>
            saved.OnlineArticleMappingId == existing.OnlineArticleMappingId &&
            saved.StoreProductName == "Neue Bio Tomate" && saved.StoreProductPrice == 3.1m && saved.Confidence == 95 &&
            saved.MatchMethod == MatchMethod.FuzzyMatch && saved.QuantityPerUnit == 4 && saved.LastUsedAt == _timeProvider.GetUtcNow() &&
            saved.FeedbackCount == 3);
    }

    [Test]
    public async Task SaveOrUpdateMappingAsync_WhenArticleNameAndStoreKeyMatchButProductCodeDiffers_CreatesNewMappingInsteadOfUpdating()
    {
        // Arrange — the matching predicate requires ArticleName AND StoreKey AND StoreProductCode to all
        // match; this proves it is not sufficient for only ArticleName and StoreKey to match (i.e. that
        // the conditions are combined with && rather than ||).
        await using var context = new InMemoryDbContext();
        var existing = new OnlineArticleMapping("Tomate", "coop", "111", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        existing.RecordFeedback(5);
        context.OnlineArticleMappings.Add(existing);
        await context.SaveChangesAsync();
        var testee = new ArticleMappingRepository(context, _timeProvider);
        var newMapping = new OnlineArticleMapping("Tomate", "coop", "222", _timeProvider.GetUtcNow());

        // Act
        await testee.SaveOrUpdateMappingAsync(newMapping);

        // Assert
        context.OnlineArticleMappings.Should().HaveCount(2);
        context.OnlineArticleMappings.Should().Contain(mapping => mapping.OnlineArticleMappingId == existing.OnlineArticleMappingId && mapping.FeedbackCount == 5);
        context.OnlineArticleMappings.Should().Contain(mapping => mapping.StoreProductCode == "222" && mapping.FeedbackCount == 0);
    }

    [Test]
    public async Task GetMappingAsync_WhenMultipleCandidatesExist_ReturnsHighestFeedbackThenHighestConfidence()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var mapping1 = new OnlineArticleMapping("Tomate", "coop", "1", _timeProvider.GetUtcNow());
        mapping1.RecordMatch(string.Empty, 0m, 99, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        mapping1.RecordFeedback();
        context.OnlineArticleMappings.Add(mapping1);
        var mapping2 = new OnlineArticleMapping("Tomate", "coop", "2", _timeProvider.GetUtcNow());
        mapping2.RecordMatch(string.Empty, 0m, 10, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        mapping2.RecordFeedback(5);
        var bestMatch = context.OnlineArticleMappings.Add(mapping2).Entity;
        var mapping3 = new OnlineArticleMapping("Tomate", "coop", "3", _timeProvider.GetUtcNow());
        mapping3.RecordMatch(string.Empty, 0m, 5, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        mapping3.RecordFeedback(5);
        context.OnlineArticleMappings.Add(mapping3);
        var mapping4 = new OnlineArticleMapping("Anderes", "coop", "4", _timeProvider.GetUtcNow());
        mapping4.RecordMatch(string.Empty, 0m, 99, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        mapping4.RecordFeedback(9);
        context.OnlineArticleMappings.Add(mapping4);
        await context.SaveChangesAsync();
        var testee = new ArticleMappingRepository(context, _timeProvider);

        // Act
        var result = await testee.GetMappingAsync("coop", "Tomate");

        // Assert
        result.Should().NotBeNull();
        result!.OnlineArticleMappingId.Should().Be(bestMatch.OnlineArticleMappingId);
    }

    [Test]
    public async Task GetMappingAsync_WhenNoMatchingMappingExists_ReturnsNull()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new ArticleMappingRepository(context, _timeProvider);

        // Act
        var result = await testee.GetMappingAsync("coop", "Tomate");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAllMappingsAsync_ReturnsAllMatchesOrderedByFeedbackThenLastUsedAtDescending()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var olderMapping = new OnlineArticleMapping("Tomate", "coop", "1", _timeProvider.GetUtcNow());
        olderMapping.RecordMatch(string.Empty, 0m, 0, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow().AddDays(-2));
        olderMapping.RecordFeedback(5);
        var older = context.OnlineArticleMappings.Add(olderMapping).Entity;
        var newerMapping = new OnlineArticleMapping("Tomate", "coop", "2", _timeProvider.GetUtcNow());
        newerMapping.RecordMatch(string.Empty, 0m, 0, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        newerMapping.RecordFeedback(5);
        var newer = context.OnlineArticleMappings.Add(newerMapping).Entity;
        var lowFeedbackMapping = new OnlineArticleMapping("Tomate", "coop", "3", _timeProvider.GetUtcNow());
        lowFeedbackMapping.RecordMatch(string.Empty, 0m, 0, MatchMethod.FuzzyMatch, null, _timeProvider.GetUtcNow());
        lowFeedbackMapping.RecordFeedback();
        var lowFeedback = context.OnlineArticleMappings.Add(lowFeedbackMapping).Entity;
        var otherArticleMapping = new OnlineArticleMapping("Anderes", "coop", "4", _timeProvider.GetUtcNow());
        otherArticleMapping.RecordFeedback(9);
        context.OnlineArticleMappings.Add(otherArticleMapping);
        await context.SaveChangesAsync();
        var testee = new ArticleMappingRepository(context, _timeProvider);

        // Act
        var result = (await testee.GetAllMappingsAsync("coop", "Tomate")).ToList();

        // Assert
        result.Select(mapping => mapping.OnlineArticleMappingId).Should().Equal(
            newer.OnlineArticleMappingId, older.OnlineArticleMappingId, lowFeedback.OnlineArticleMappingId);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
