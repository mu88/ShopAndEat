using BizDbAccess;
using DataLayer.EfClasses;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Features.ShoppingAgent.Adapters;
using ShoppingAgent.Services;

namespace Tests.Unit.ShopAndEat.Features.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ServerPreferencesAdapterTests
{
    [Test]
    public async Task GetAllPreferencesAsync_ReturnsMappedDtos()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.GetAllPreferencesAsync(null, null, default).Returns(
        [
            new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, null) { Value = "true" },
            new ShoppingPreference("article:Tofu", "confirmed_product", PreferenceSource.AgentLearned, "coop") { Value = "Organic Tofu" },
        ]);
        var testee = CreateTestee(repo);

        // Act
        var result = await testee.GetAllPreferencesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Scope.Should().Be("global");
        result[0].Key.Should().Be("prefer_bio");
        result[0].Value.Should().Be("true");
        result[1].Scope.Should().Be("article:Tofu");
        result[1].StoreKey.Should().Be("coop");
    }

    [Test]
    public async Task GetAllPreferencesAsync_FiltersWithStoreKey()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.GetAllPreferencesAsync(null, "coop", default).Returns([]);
        var testee = CreateTestee(repo);

        // Act
        await testee.GetAllPreferencesAsync("coop");

        // Assert
        await repo.Received(1).GetAllPreferencesAsync(null, "coop", default);
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_UsesArticleScope()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.GetAllPreferencesAsync("article:Tofu", null, default).Returns(
        [
            new ShoppingPreference("article:Tofu", "confirmed_product", PreferenceSource.AgentLearned, null) { Value = "Karma Tofu" },
        ]);
        var testee = CreateTestee(repo);

        // Act
        var result = await testee.GetPreferencesForArticleAsync("Tofu");

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("confirmed_product");
        await repo.Received(1).GetAllPreferencesAsync("article:Tofu", null, default);
    }

    [Test]
    public async Task GetPreferencesForArticleAsync_IncludesStoreKey_WhenProvided()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.GetAllPreferencesAsync("article:Milk", "migros", default).Returns([]);
        var testee = CreateTestee(repo);

        // Act
        await testee.GetPreferencesForArticleAsync("Milk", "migros");

        // Assert
        await repo.Received(1).GetAllPreferencesAsync("article:Milk", "migros", default);
    }

    [Test]
    public async Task SavePreferenceAsync_CallsUpsert_WithCorrectEntity()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        var testee = CreateTestee(repo);
        var dto = new PreferenceDto { Scope = "global", Key = "prefer_bio", Value = "true", StoreKey = null };

        // Act
        await testee.SavePreferenceAsync(dto);

        // Assert
        await repo.Received(1).UpsertPreferenceAsync(
            Arg.Is<ShoppingPreference>(p =>
                string.Equals(p.Scope, "global", StringComparison.Ordinal) &&
                string.Equals(p.Key, "prefer_bio", StringComparison.Ordinal) &&
                string.Equals(p.Value, "true", StringComparison.Ordinal) &&
                p.Source == PreferenceSource.AgentLearned),
            default);
    }

    [Test]
    public async Task DeletePreferenceAsync_DelegatesToRepository()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.DeletePreferenceAsync("global", "prefer_bio", null, default).Returns(true);
        var testee = CreateTestee(repo);

        // Act
        var result = await testee.DeletePreferenceAsync("global", "prefer_bio");

        // Assert
        result.Should().BeTrue();
        await repo.Received(1).DeletePreferenceAsync("global", "prefer_bio", null, default);
    }

    [Test]
    public async Task DeletePreferenceAsync_PassesStoreKey_WhenProvided()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.DeletePreferenceAsync("article:Tofu", "confirmed_product", "coop", default).Returns(true);
        var testee = CreateTestee(repo);

        // Act
        await testee.DeletePreferenceAsync("article:Tofu", "confirmed_product", "coop");

        // Assert
        await repo.Received(1).DeletePreferenceAsync("article:Tofu", "confirmed_product", "coop", default);
    }

    [Test]
    public async Task GetAllPreferencesAsync_MapsNullValueToEmptyString()
    {
        // Arrange
        var repo = Substitute.For<IPreferencesRepository>();
        repo.GetAllPreferencesAsync(null, null, default).Returns(
        [
            new ShoppingPreference("global", "some_key", PreferenceSource.UserConfirmed, null) { Value = null },
        ]);
        var testee = CreateTestee(repo);

        // Act
        var result = await testee.GetAllPreferencesAsync();

        // Assert
        result[0].Value.Should().BeEmpty();
    }

    private static ServerPreferencesAdapter CreateTestee(IPreferencesRepository repo) =>
        new(repo, NullLogger<ServerPreferencesAdapter>.Instance);
}
