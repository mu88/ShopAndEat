using System.Net;
using System.Net.Http.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Containers;
using DTO.ShoppingPreference;
using DTO.ShoppingSession;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests.System;

[Category("System")]
public class ShoppingFeatureSystemTests
{
    private static CancellationTokenSource _cancellationTokenSource;
    private static CancellationToken _cancellationToken;
    private static DockerClient _dockerClient;
    private static IContainer _container;
    private static HttpClient _httpClient;

    [OneTimeSetUp]
    public static async Task OneTimeSetup()
    {
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        _cancellationToken = _cancellationTokenSource.Token;
        _dockerClient = new DockerClientConfiguration().CreateClient();

        var containerImageTag = DockerTestInfrastructure.GenerateContainerImageTag();
        await DockerTestInfrastructure.BuildDockerImageOfAppAsync(containerImageTag, _cancellationToken);
        _container = await DockerTestInfrastructure.StartAppInContainersAsync(containerImageTag, _cancellationToken);
        _httpClient = new HttpClient { BaseAddress = DockerTestInfrastructure.GetAppBaseAddress(_container) };
    }

    [OneTimeTearDown]
    public static async Task OneTimeTeardown()
    {
        _httpClient?.Dispose();

        try
        {
            if (_container is not null)
            {
                await _container.StopAsync(CancellationToken.None);
                await _container.DisposeAsync();

                // Only delete the image when tests passed (keep for investigation on failure)
                var allPassed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
                if (allPassed && !string.IsNullOrWhiteSpace(_container.Image.FullName))
                {
                    await _dockerClient.Images.DeleteImageAsync(
                        _container.Image.FullName,
                        new ImageDeleteParameters { Force = true },
                        CancellationToken.None);
                }
            }
        }
        finally
        {
            _dockerClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
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
    public async Task WasmApp_ShouldServeStaticFilesInDocker()
    {
        // Act
        var response = await _httpClient.GetAsync("/shopAndEat/shopping/", _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because the Blazor WASM shopping page should be served");
        (await response.Content.ReadAsStringAsync(_cancellationToken))
            .Should().ContainAny("blazor", "_framework", "because the page should reference the Blazor framework");
    }

    [Test]
    public async Task WasmRuntime_ShouldBeLoadableInDocker()
    {
        // Act - verify that the WASM framework entry point is actually servable
        var response = await _httpClient.GetAsync("/shopAndEat/_framework/dotnet.js", _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "because the Blazor WASM runtime (dotnet.js) must be loadable for the shopping page to work");
    }

    private static async Task CreatePreference(string scope, string key, string value)
    {
        var request = new PreferenceRequest { Scope = scope, Key = key, Value = value };
        var content = global::System.Net.Http.Json.JsonContent.Create(request);
        var response = await _httpClient.PostAsync("/shopAndEat/api/preferences", content, _cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"because creating preference '{scope}/{key}' should succeed");
    }

    private static async Task<List<PreferenceResponse>> GetAllPreferences()
    {
        var preferences = await _httpClient.GetFromJsonAsync<List<PreferenceResponse>>("/shopAndEat/api/preferences", _cancellationToken);
        preferences.Should().NotBeNull("because GET preferences should return a valid response");
        return preferences!;
    }

    private static async Task DeletePreference(string scope, string key)
    {
        var response = await _httpClient.DeleteAsync($"/shopAndEat/api/preferences?scope={Uri.EscapeDataString(scope)}&key={Uri.EscapeDataString(key)}", _cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, $"because deleting preference '{scope}/{key}' should succeed");
    }

    private static async Task VerifyPreferencesCount(int expectedCount)
    {
        var preferences = await GetAllPreferences();
        preferences.Should().HaveCount(expectedCount,
            $"because there should be {expectedCount} preference(s) at this point in the test");
    }

    private static async Task<HttpResponseMessage> GetUnits()
        => await _httpClient.GetAsync("/shopAndEat/api/units", _cancellationToken);
}
