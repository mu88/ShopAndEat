using System.Net;
using System.Net.Http.Json;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.ShoppingPreference;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class PreferencesApiTests
{
    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoPreferencesExist()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences");

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task Post_CreatesPreference()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        var request = new PreferenceRequest { Scope = "global", Key = "prefer_bio", Value = "true", Source = PreferenceSource.UserConfirmed };

        // Act
        var response = await client.PostAsJsonAsync("shopAndEat/api/preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferences = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=global");
        preferences.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Scope = "global", Key = "prefer_bio", Value = "true" });
    }

    [Test]
    public async Task Post_UpdatesExistingPreference_WhenSameScopeAndKey()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        var first = new PreferenceRequest { Scope = "article:Tofu", Key = "confirmed_product", Value = "Organic Tofu Nature", Source = PreferenceSource.UserConfirmed };
        var second = new PreferenceRequest { Scope = "article:Tofu", Key = "confirmed_product", Value = "Karma Tofu", Source = PreferenceSource.AgentLearned };
        await client.PostAsJsonAsync("shopAndEat/api/preferences", first);

        // Act
        await client.PostAsJsonAsync("shopAndEat/api/preferences", second);

        // Assert
        var preferences = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=article:Tofu");
        preferences.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Value = "Karma Tofu", Source = "UserConfirmed", UsageCount = 1 });
    }

    [Test]
    public async Task GetAll_FiltersByScope()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        await client.PostAsJsonAsync("shopAndEat/api/preferences",
            new PreferenceRequest { Scope = "global", Key = "prefer_bio", Value = "true" });
        await client.PostAsJsonAsync("shopAndEat/api/preferences",
            new PreferenceRequest { Scope = "article:Tofu", Key = "confirmed_product", Value = "Organic Tofu" });

        // Act
        var globalPrefs = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=global");
        var articlePrefs = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=article:Tofu");

        // Assert
        globalPrefs.Should().ContainSingle().Which.Key.Should().Be("prefer_bio");
        articlePrefs.Should().ContainSingle().Which.Key.Should().Be("confirmed_product");
    }

    [Test]
    public async Task Delete_RemovesPreference()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        await client.PostAsJsonAsync("shopAndEat/api/preferences",
            new PreferenceRequest { Scope = "global", Key = "prefer_bio", Value = "true" });

        // Act
        var response = await client.DeleteAsync("shopAndEat/api/preferences?scope=global&key=prefer_bio");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var preferences = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=global");
        preferences.Should().BeEmpty();
    }

    [Test]
    public async Task Delete_ReturnsNotFound_WhenPreferenceDoesNotExist()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.DeleteAsync("shopAndEat/api/preferences?scope=nonexistent&key=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAll_OrdersByUsageCountDescending()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        using (var scope = webApplicationFactory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EfCoreContext>();
            context.ShoppingPreferences.Add(new ShoppingPreference("global", "a", PreferenceSource.UserConfirmed, null) { Value = "1", UsageCount = 5 });
            context.ShoppingPreferences.Add(new ShoppingPreference("global", "b", PreferenceSource.UserConfirmed, null) { Value = "2", UsageCount = 10 });
            context.ShoppingPreferences.Add(new ShoppingPreference("global", "c", PreferenceSource.UserConfirmed, null) { Value = "3", UsageCount = 1 });
            await context.SaveChangesAsync();
        }

        var client = webApplicationFactory.CreateClient();

        // Act
        var preferences = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=global");

        // Assert
        preferences.Should().HaveCount(3);
        preferences.Select(preference => preference.Key).Should().ContainInOrder("b", "a", "c");
    }

    [Test]
    public async Task GetAll_FiltersByStoreKey_IncludingNullStoreKey()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        await client.PostAsJsonAsync("shopAndEat/api/preferences",
            new PreferenceRequest { Scope = "global", Key = "shop_pref", Value = "yes", StoreKey = "coop" });
        await client.PostAsJsonAsync("shopAndEat/api/preferences",
            new PreferenceRequest { Scope = "global", Key = "general_pref", Value = "yes", StoreKey = null });

        // Act
        var result = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?storeKey=coop");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Key == "shop_pref" && p.StoreKey == "coop");
        result.Should().Contain(p => p.Key == "general_pref" && p.StoreKey == null);
    }

    [Test]
    public async Task Post_IncrementsUsageCount_OnUpdate()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        var request = new PreferenceRequest { Scope = "article:Rice", Key = "confirmed_product", Value = "Organic Rice 1kg", Source = PreferenceSource.UserConfirmed };
        await client.PostAsJsonAsync("shopAndEat/api/preferences", request);

        // Act
        await client.PostAsJsonAsync("shopAndEat/api/preferences", new PreferenceRequest { Scope = "article:Rice", Key = "confirmed_product", Value = "Demeter Rice 500g", Source = PreferenceSource.UserConfirmed });

        // Assert
        var preferences = await client.GetFromJsonAsync<List<PreferenceResponse>>("shopAndEat/api/preferences?scope=article:Rice");
        preferences.Should().ContainSingle()
            .Which.UsageCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Post_Returns400_WhenScopeIsEmpty()
    {
        // Arrange
        await using var webApplicationFactory = new CustomWebApplicationFactory();
        var client = webApplicationFactory.CreateClient();
        var request = new PreferenceRequest { Scope = string.Empty, Key = "some_key", Value = "some_value" };

        // Act
        var response = await client.PostAsJsonAsync("shopAndEat/api/preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
