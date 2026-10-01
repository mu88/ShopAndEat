using BizDbAccess.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class PreferencesRepositoryTests
{
    [Test]
    public async Task GetAllPreferencesAsync_WithoutFilters_ReturnsAllPreferencesOrderedByUsageCountDescending()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var lessUsed = new ShoppingPreferenceBuilder().WithDefaults().Build();
        lessUsed.Value = "1";
        lessUsed.UsageCount = 1;
        var mostUsed = new ShoppingPreferenceBuilder().WithDefaults().Build();
        mostUsed.Value = "2";
        mostUsed.UsageCount = 9;
        context.ShoppingPreferences.Add(lessUsed);
        context.ShoppingPreferences.Add(mostUsed);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.GetAllPreferencesAsync(null, null);

        // Assert
        result.Select(preference => preference.ShoppingPreferenceId).Should().Equal(
            mostUsed.ShoppingPreferenceId, lessUsed.ShoppingPreferenceId);
    }

    [Test]
    public async Task GetAllPreferencesAsync_WithScopeFilter_ReturnsOnlyPreferencesForThatScope()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        context.ShoppingPreferences.Add(new ShoppingPreference("global", "a", PreferenceSource.UserConfirmed, null) { Value = "1" });
        var articleScoped = context.ShoppingPreferences.Add(new ShoppingPreference("article:Tofu", "b", PreferenceSource.UserConfirmed, null) { Value = "2" }).Entity;
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.GetAllPreferencesAsync("article:Tofu", null);

        // Assert
        result.Should().ContainSingle(preference => preference.ShoppingPreferenceId == articleScoped.ShoppingPreferenceId);
    }

    [Test]
    public async Task GetAllPreferencesAsync_WithEmptyScope_DoesNotApplyScopeFilter()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var global = context.ShoppingPreferences.Add(new ShoppingPreference("global", "a", PreferenceSource.UserConfirmed, null) { Value = "1" }).Entity;
        var articleScoped = context.ShoppingPreferences.Add(new ShoppingPreference("article:Tofu", "b", PreferenceSource.UserConfirmed, null) { Value = "2" }).Entity;
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.GetAllPreferencesAsync(string.Empty, null);

        // Assert
        result.Select(preference => preference.ShoppingPreferenceId).Should().BeEquivalentTo(
            [global.ShoppingPreferenceId, articleScoped.ShoppingPreferenceId]);
    }

    [Test]
    public async Task GetAllPreferencesAsync_WithStoreKeyFilter_IncludesStoreSpecificAndOverarchingPreferences()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var coopSpecific = context.ShoppingPreferences.Add(new ShoppingPreference("global", "a", PreferenceSource.UserConfirmed, "coop") { Value = "1" }).Entity;
        var overarching = context.ShoppingPreferences.Add(new ShoppingPreference("global", "b", PreferenceSource.UserConfirmed, null) { Value = "2" }).Entity;
        context.ShoppingPreferences.Add(new ShoppingPreference("global", "c", PreferenceSource.UserConfirmed, "migros") { Value = "3" });
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.GetAllPreferencesAsync(null, "coop");

        // Assert
        result.Select(preference => preference.ShoppingPreferenceId).Should().BeEquivalentTo(
            [coopSpecific.ShoppingPreferenceId, overarching.ShoppingPreferenceId]);
    }

    [Test]
    public async Task UpsertPreferenceAsync_WhenNoMatchingPreferenceExists_InsertsNewPreference()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new PreferencesRepository(context);
        var preference = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true" };

        // Act
        await testee.UpsertPreferenceAsync(preference);

        // Assert
        context.ShoppingPreferences.Should().ContainSingle(saved =>
            saved.Scope == "global" && saved.Key == "prefer_bio" && saved.StoreKey == "coop" && saved.Value == "true" && saved.UsageCount == 0);
    }

    [Test]
    public async Task UpsertPreferenceAsync_WhenKeyAndStoreKeyMatchButScopeDiffers_InsertsNewPreferenceInsteadOfUpdating()
    {
        // Arrange — proves Scope is also required to match (not just Key+StoreKey), i.e. that the first
        // two conditions are combined with && rather than ||.
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true", UsageCount = 3 };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);
        var newPreference = new ShoppingPreference("article:Tofu", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "false" };

        // Act
        await testee.UpsertPreferenceAsync(newPreference);

        // Assert
        context.ShoppingPreferences.Should().HaveCount(2);
        context.ShoppingPreferences.Should().Contain(preference => preference.ShoppingPreferenceId == existing.ShoppingPreferenceId && preference.UsageCount == 3);
        context.ShoppingPreferences.Should().Contain(preference => preference.Scope == "article:Tofu" && preference.UsageCount == 0);
    }

    [Test]
    public async Task UpsertPreferenceAsync_WhenMatchingPreferenceExists_UpdatesValueAndIncrementsUsageCount()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "false", UsageCount = 3 };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);
        var updated = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true" };

        // Act
        await testee.UpsertPreferenceAsync(updated);

        // Assert
        context.ShoppingPreferences.Should().ContainSingle(saved =>
            saved.ShoppingPreferenceId == existing.ShoppingPreferenceId && saved.Value == "true" && saved.UsageCount == 4);
    }

    [Test]
    public async Task UpsertPreferenceAsync_WhenScopeAndKeyMatchButStoreKeyDiffers_InsertsNewPreferenceInsteadOfUpdating()
    {
        // Arrange — the matching predicate requires Scope AND Key AND StoreKey to all match; this proves
        // it is not sufficient for only Scope and Key to match (i.e. the conditions use && rather than ||).
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true", UsageCount = 3 };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);
        var newPreference = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "migros") { Value = "false" };

        // Act
        await testee.UpsertPreferenceAsync(newPreference);

        // Assert
        context.ShoppingPreferences.Should().HaveCount(2);
        context.ShoppingPreferences.Should().Contain(preference => preference.ShoppingPreferenceId == existing.ShoppingPreferenceId && preference.UsageCount == 3);
        context.ShoppingPreferences.Should().Contain(preference => preference.StoreKey == "migros" && preference.UsageCount == 0);
    }

    [Test]
    public async Task DeletePreferenceAsync_WhenPreferenceExists_RemovesItAndReturnsTrue()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true" };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio", "coop");

        // Assert
        result.Should().BeTrue();
        context.ShoppingPreferences.Should().NotContain(existing);
    }

    [Test]
    public async Task DeletePreferenceAsync_WhenPreferenceDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio", "coop");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task DeletePreferenceAsync_WhenScopeAndKeyMatchButStoreKeyDiffers_ReturnsFalseWithoutDeleting()
    {
        // Arrange — the matching predicate requires Scope AND Key AND StoreKey to all match; this proves
        // it is not sufficient for only Scope and Key to match (i.e. the last condition uses && rather than ||).
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true" };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio", "migros");

        // Assert
        result.Should().BeFalse();
        context.ShoppingPreferences.Should().ContainSingle(preference => preference.ShoppingPreferenceId == existing.ShoppingPreferenceId);
    }

    [Test]
    public async Task DeletePreferenceAsync_WhenKeyAndStoreKeyMatchButScopeDiffers_ReturnsFalseWithoutDeleting()
    {
        // Arrange — proves Scope is also required to match (not just Key+StoreKey), i.e. that the first
        // two conditions are combined with && rather than ||.
        await using var context = new InMemoryDbContext();
        var existing = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, "coop") { Value = "true" };
        context.ShoppingPreferences.Add(existing);
        await context.SaveChangesAsync();
        var testee = new PreferencesRepository(context);

        // Act
        var result = await testee.DeletePreferenceAsync("article:Tofu", "prefer_bio", "coop");

        // Assert
        result.Should().BeFalse();
        context.ShoppingPreferences.Should().ContainSingle(preference => preference.ShoppingPreferenceId == existing.ShoppingPreferenceId);
    }
}
