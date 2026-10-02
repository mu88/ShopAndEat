using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    private static HttpClient _httpClient = null!;

    private CancellationTokenSource _cancellationTokenSource = null!;
    private CancellationToken _cancellationToken;

    [OneTimeSetUp]
    public static void OneTimeSetup()
    {
        // Shared across all tests in this class (not per-test) to avoid socket exhaustion from
        // creating a new HttpClient per test (IDISP014) - BaseAddress never changes between tests.
        _httpClient = new HttpClient { BaseAddress = SystemTestsFixture.AppBaseAddress };
    }

    [OneTimeTearDown]
    public static void OneTimeTeardown()
    {
        _httpClient.Dispose();
    }

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
        // Arrange - uses the shared _httpClient created in OneTimeSetup

        // Act
        var healthCheckResponse = await _httpClient.GetAsync("healthz", _cancellationToken);
        var appResponse = await _httpClient.GetAsync("/", _cancellationToken);

        // Assert
        healthCheckResponse.Should().Be200Ok();
        await HealthCheckShouldBeHealthyAsync(healthCheckResponse, _cancellationToken);
        await AppShouldRunAsync(appResponse, _cancellationToken);
    }

    [Test]
    public async Task ShoppingFeature_ShouldBeAccessibleInDocker()
    {
        // Arrange & Act & Assert - Preferences CRUD roundtrip, uses the shared _httpClient from OneTimeSetup
        var getPreferences1 = await _httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences1.Should().Be200Ok();
        using var doc1 = JsonDocument.Parse(await getPreferences1.Content.ReadAsStringAsync(_cancellationToken));
        doc1.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);

        var postPreferences = await _httpClient.PostAsync(
            "/shopAndEat/api/preferences",
            new StringContent("""{"scope":"test","key":"testKey","value":"testValue"}""", Encoding.UTF8, "application/json"),
            _cancellationToken);
        postPreferences.Should().Be200Ok();

        var getPreferences2 = await _httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences2.Should().Be200Ok();
        using var doc2 = JsonDocument.Parse(await getPreferences2.Content.ReadAsStringAsync(_cancellationToken));
        doc2.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var deletePreferences = await _httpClient.DeleteAsync("/shopAndEat/api/preferences?scope=test&key=testKey", _cancellationToken);
        deletePreferences.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getPreferences3 = await _httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences3.Should().Be200Ok();

        // Act & Assert - Sessions and Units
        var getSessions = await _httpClient.GetAsync("/shopAndEat/api/shopping/sessions", _cancellationToken);
        getSessions.Should().Be200Ok();

        var getUnits = await _httpClient.GetAsync("/shopAndEat/api/units", _cancellationToken);
        getUnits.Should().Be200Ok();

        // Act & Assert - WASM static files
        var shoppingPage = await _httpClient.GetAsync("/shopAndEat/shopping/", _cancellationToken);
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
