using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    private CancellationTokenSource _cancellationTokenSource = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        _cancellationToken = _cancellationTokenSource.Token;
    }

    [TearDown]
    public void Teardown()
    {
        _cancellationTokenSource.Dispose();
    }

    [Test]
    public async Task AppRunningInDocker_ShouldBeHealthy()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = SystemTestsFixture.AppBaseAddress };

        // Act
        var healthCheckResponse = await httpClient.GetAsync("healthz", _cancellationToken);
        var appResponse = await httpClient.GetAsync("/", _cancellationToken);

        // Assert
        healthCheckResponse.Should().Be200Ok();
        await HealthCheckShouldBeHealthyAsync(healthCheckResponse, _cancellationToken);
        await AppShouldRunAsync(appResponse, _cancellationToken);
    }

    [Test]
    public async Task ShoppingFeature_ShouldBeAccessibleInDocker()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = SystemTestsFixture.AppBaseAddress };

        // Act & Assert - Preferences CRUD roundtrip
        var getPreferences1 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences1.Should().Be200Ok();
        using var doc1 = JsonDocument.Parse(await getPreferences1.Content.ReadAsStringAsync(_cancellationToken));
        doc1.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);

        var postPreferences = await httpClient.PostAsync(
            "/shopAndEat/api/preferences",
            new StringContent("""{"scope":"test","key":"testKey","value":"testValue"}""", Encoding.UTF8, "application/json"),
            _cancellationToken);
        postPreferences.Should().Be200Ok();

        var getPreferences2 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences2.Should().Be200Ok();
        using var doc2 = JsonDocument.Parse(await getPreferences2.Content.ReadAsStringAsync(_cancellationToken));
        doc2.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var deletePreferences = await httpClient.DeleteAsync("/shopAndEat/api/preferences?scope=test&key=testKey", _cancellationToken);
        deletePreferences.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getPreferences3 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences3.Should().Be200Ok();

        // Act & Assert - Sessions and Units
        var getSessions = await httpClient.GetAsync("/shopAndEat/api/shopping/sessions", _cancellationToken);
        getSessions.Should().Be200Ok();

        var getUnits = await httpClient.GetAsync("/shopAndEat/api/units", _cancellationToken);
        getUnits.Should().Be200Ok();

        // Act & Assert - WASM static files
        var shoppingPage = await httpClient.GetAsync("/shopAndEat/shopping/", _cancellationToken);
        shoppingPage.Should().Be200Ok();
        (await shoppingPage.Content.ReadAsStringAsync(_cancellationToken)).Should().ContainAny("blazor", "_framework");
    }

    private static async Task AppShouldRunAsync(HttpResponseMessage appResponse, CancellationToken cancellationToken)
    {
        appResponse.Should().Be200Ok();
        (await appResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Contain("<title>ShopAndEat</title>");
    }

    private static async Task HealthCheckShouldBeHealthyAsync(HttpResponseMessage healthCheckResponse, CancellationToken cancellationToken)
    {
        healthCheckResponse.Should().Be200Ok();
        (await healthCheckResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Be("Healthy");
    }
}
