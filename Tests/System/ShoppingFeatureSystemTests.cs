using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using DTO.ShoppingPreference;
using DTO.ShoppingSession;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.System;

[Category("System")]
public class ShoppingFeatureSystemTests
{
    private static readonly HttpClient _httpClient = new() { BaseAddress = SystemTestsFixture.AppBaseAddress };

    private CancellationTokenSource _cancellationTokenSource = null!;
    private CancellationToken _cancellationToken;

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
        _cancellationTokenSource?.Dispose();
    }

    [Test]
    public async Task PreferencesCrud_ShouldWorkInDocker()
    {
        // Act & Assert
        await VerifyPreferencesCount(0);

        await CreatePreference("test", "testKey", "testValue");
        await VerifyPreferencesCount(1);

        await DeletePreference("test", "testKey");
        await VerifyPreferencesCount(0);
    }

    [Test]
    public async Task SessionsApi_ShouldBeAccessibleInDocker()
    {
        // Act
        var sessions = await _httpClient.GetFromJsonAsync<List<SessionResponse>>("/shopAndEat/api/shopping/sessions", _cancellationToken);

        // Assert
        sessions.Should().NotBeNull("because the sessions API should be accessible in Docker");
    }

    [Test]
    public async Task UnitsApi_ShouldBeAccessibleInDocker()
    {
        // Act
        var response = await GetUnits();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because the units API should be accessible in Docker");
    }

    [Test]
    public async Task BlazorServerApp_ShouldServeShoppingPageInDocker()
    {
        // Act
        var response = await _httpClient.GetAsync("/shopAndEat/shopping/", _cancellationToken);
        var html = await response.Content.ReadAsStringAsync(_cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because the Blazor Server shopping page should be served");

        // With InteractiveServer render mode, the first response is SSR pre-rendered,
        // so the component markup (including localised text) must be present immediately.
        html.Should().Contain("Shopping Assistant", "because the Home component is pre-rendered SSR and its content must be in the initial HTML");
        html.Should().Contain("blazor.server.js", "because the Blazor Server runtime script must be referenced for subsequent interactivity");
    }

    [Test]
    public async Task BlazorServerRuntime_ShouldBeServableInDocker()
    {
        // Act - verify that the Blazor Server framework script is actually servable
        var response = await _httpClient.GetAsync("/shopAndEat/_framework/blazor.server.js", _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because the Blazor Server runtime (blazor.server.js) must be loadable for the shopping page to work");
    }

    private async Task CreatePreference(string scope, string key, string value)
    {
        var request = new PreferenceRequest { Scope = scope, Key = key, Value = value };
        var content = JsonContent.Create(request);
        var response = await _httpClient.PostAsync("/shopAndEat/api/preferences", content, _cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"because creating preference '{scope}/{key}' should succeed");
    }

    private async Task<List<PreferenceResponse>> GetAllPreferences()
    {
        var preferences = await _httpClient.GetFromJsonAsync<List<PreferenceResponse>>("/shopAndEat/api/preferences", _cancellationToken);
        preferences.Should().NotBeNull("because GET preferences should return a valid response");
        return preferences!;
    }

    private async Task DeletePreference(string scope, string key)
    {
        var response = await _httpClient.DeleteAsync($"/shopAndEat/api/preferences?scope={Uri.EscapeDataString(scope)}&key={Uri.EscapeDataString(key)}", _cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, $"because deleting preference '{scope}/{key}' should succeed");
    }

    private async Task VerifyPreferencesCount(int expectedCount)
    {
        var preferences = await GetAllPreferences();
        preferences.Should().HaveCount(expectedCount,
            $"because there should be {expectedCount.ToString(CultureInfo.InvariantCulture)} preference(s) at this point in the test");
    }

    private async Task<HttpResponseMessage> GetUnits()
        => await _httpClient.GetAsync("/shopAndEat/api/units", _cancellationToken);
}
