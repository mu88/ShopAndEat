using System.Net;
using System.Net.Http.Json;
using DataLayer.EfClasses;
using DTO.OnlineArticleMapping;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Integration.ShopAndEat;

[TestFixture]
[Category("Integration")]
public class OnlineShoppingApiTests
{
    private const string StoreKey = "coop";
    private const string BasePath = "shopAndEat/api/shopping";

    [Test]
    public async Task SaveMapping_CreatesNew()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(MappingsPath(StoreKey), BuildMapping("Tofu"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task SaveMapping_UpdatesExisting_AndIncrementsFeedbackCount()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var mapping = BuildMapping("Tofu");
        await client.PostAsJsonAsync(MappingsPath(StoreKey), mapping);
        var updated = BuildMapping("Tofu") with { StoreProductName = "Updated Tofu" };

        // Act
        await client.PostAsJsonAsync(MappingsPath(StoreKey), updated);

        // Assert
        var result = await client.GetFromJsonAsync<ExistingOnlineArticleMappingDto>(GetMappingPath(StoreKey, "Tofu"));
        result!.FeedbackCount.Should().Be(1);
        result.StoreProductName.Should().Be("Updated Tofu");
    }

    [Test]
    public async Task GetMapping_ReturnsMapping_AfterSave()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(MappingsPath(StoreKey), BuildMapping("Carrots"));

        // Act
        var result = await client.GetFromJsonAsync<ExistingOnlineArticleMappingDto>(GetMappingPath(StoreKey, "Carrots"));

        // Assert
        result.Should().NotBeNull();
        result!.ArticleName.Should().Be("Carrots");
        result.StoreKey.Should().Be(StoreKey);
    }

    [Test]
    public async Task GetMapping_Returns404_WhenNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(GetMappingPath(StoreKey, "NonExistentArticle"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMappings_ReturnsAll_ForStoreAndArticle()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(MappingsPath(StoreKey), BuildMapping("Milk", "SKU-A1"));
        await client.PostAsJsonAsync(MappingsPath(StoreKey), BuildMapping("Milk", "SKU-A2"));

        // Act
        var result = await client.GetFromJsonAsync<List<ExistingOnlineArticleMappingDto>>(GetAllMappingsPath(StoreKey, "Milk"));

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.ArticleName.Should().Be("Milk"));
    }

    private static string MappingsPath(string storeKey) => $"{BasePath}/{storeKey}/mappings";

    private static string GetMappingPath(string storeKey, string articleName) => $"{BasePath}/{storeKey}/mappings/{Uri.EscapeDataString(articleName)}";

    private static string GetAllMappingsPath(string storeKey, string articleName) => $"{BasePath}/{storeKey}/mappings/{Uri.EscapeDataString(articleName)}/all";

    private static NewOnlineArticleMappingDto BuildMapping(string articleName, string productCode = "SKU-001") => new()
    {
        ArticleName = articleName,
        StoreProductCode = productCode,
        StoreProductName = $"Product for {articleName}",
        StoreProductPrice = 1.99m,
        Confidence = 80f,
        MatchMethod = MatchMethod.FuzzyMatch,
    };
}
