using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests.System;

[Category("System")]
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "System tests create isolated HttpClient instances per test by design")]
public class SystemTests
{
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private DockerClient _dockerClient;
    private IContainer _container;

    [SetUp]
    public void Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        _cancellationToken = _cancellationTokenSource.Token;
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    [TearDown]
    public async Task Teardown()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
        {
            return; // no need to clean up on GitHub Actions runners
        }

        // If the test passed, clean up the container and image. Otherwise, keep them for investigation.
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed && _container is not null)
        {
            await _container.StopAsync(_cancellationToken);
            await _container.DisposeAsync();
            await _dockerClient.Images.DeleteImageAsync(_container.Image.FullName, new ImageDeleteParameters { Force = true }, _cancellationToken);
        }

        _dockerClient.Dispose();
        _cancellationTokenSource.Dispose();
    }

    [Test]
    public async Task AppRunningInDocker_ShouldBeHealthy()
    {
        // Arrange
        var containerImageTag = GenerateContainerImageTag();
        await BuildDockerImageOfAppAsync(containerImageTag, _cancellationToken);
        _container = await StartAppInContainersAsync(containerImageTag, _cancellationToken);
        var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(_container) };

        // Act
        var healthCheckResponse = await httpClient.GetAsync("healthz", _cancellationToken);
        var appResponse = await httpClient.GetAsync("/", _cancellationToken);
        var healthCheckToolResult = await _container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", "http://127.0.0.1:8080/shopAndEat/healthz"], _cancellationToken);

        // Assert
        await LogsShouldNotContainWarningsAsync(_container, _cancellationToken);
        await HealthCheckShouldBeHealthyAsync(healthCheckResponse, _cancellationToken);
        await AppShouldRunAsync(appResponse, _cancellationToken);
        healthCheckToolResult.ExitCode.Should().Be(0);
    }

    [Test]
    public async Task ShoppingFeature_ShouldBeAccessibleInDocker()
    {
        // Arrange
        var containerImageTag = GenerateContainerImageTag();
        await BuildDockerImageOfAppAsync(containerImageTag, _cancellationToken);
        _container = await StartAppInContainersAsync(containerImageTag, _cancellationToken);
        var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(_container) };

        // Act & Assert - Preferences CRUD roundtrip
        var getPreferences1 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences1.Should().Be200Ok();
        using var doc1 = JsonDocument.Parse(await getPreferences1.Content.ReadAsStringAsync(_cancellationToken));
        doc1.RootElement.GetArrayLength().Should().Be(0);

        var postPreferences = await httpClient.PostAsync(
            "/shopAndEat/api/preferences",
            new StringContent("""{"scope":"test","key":"testKey","value":"testValue"}""", Encoding.UTF8, "application/json"),
            _cancellationToken);
        postPreferences.Should().Be200Ok();

        var getPreferences2 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences2.Should().Be200Ok();
        using var doc2 = JsonDocument.Parse(await getPreferences2.Content.ReadAsStringAsync(_cancellationToken));
        doc2.RootElement.GetArrayLength().Should().Be(1);

        var deletePreferences = await httpClient.DeleteAsync("/shopAndEat/api/preferences?scope=test&key=testKey", _cancellationToken);
        deletePreferences.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getPreferences3 = await httpClient.GetAsync("/shopAndEat/api/preferences", _cancellationToken);
        getPreferences3.Should().Be200Ok();
        using var doc3 = JsonDocument.Parse(await getPreferences3.Content.ReadAsStringAsync(_cancellationToken));
        doc3.RootElement.GetArrayLength().Should().Be(0);

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

    private static async Task BuildDockerImageOfAppAsync(string containerImageTag, CancellationToken cancellationToken)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var projectFile = Path.Join(rootDirectory.FullName, "ShopAndEat", "ShopAndEat.csproj");
        var buildResult = await Cli.Wrap("dotnet")
            .WithArguments([
                "publish",
                $"{projectFile}",
                "--os",
                "linux",
                "--arch",
                "amd64",
                "/t:PublishContainersForMultipleFamilies",
                $"/p:ReleaseVersion={containerImageTag}",
                "/p:IsRelease=false",
                "/p:DoNotApplyGitHubScope=true"
            ])
            .ExecuteBufferedAsync(cancellationToken);
        buildResult.IsSuccess.Should().BeTrue();
        Console.WriteLine(buildResult.StandardOutput);
    }

    private static async Task<IContainer> StartAppInContainersAsync(string containerImageTag, CancellationToken cancellationToken)
    {
        Console.WriteLine("Building and starting network");
        var network = new NetworkBuilder().Build();
        await network.CreateAsync(cancellationToken);
        Console.WriteLine("Network started");

        Console.WriteLine("Building and starting app container");
        var container = BuildAppContainer(network, containerImageTag);
        await container.StartAsync(cancellationToken);
        Console.WriteLine("App container started");

        return container;
    }

    private static IContainer BuildAppContainer(INetwork network, string containerImageTag)
        => new ContainerBuilder($"shopandeat:{containerImageTag}-chiseled")
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Content root path: /app",
                    strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))) // as it's a chiseled container, waiting for the port does not work
            .Build();

    private static Uri GetAppBaseAddress(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}/shopAndEat");

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

    private static async Task LogsShouldNotContainWarningsAsync(IContainer container, CancellationToken cancellationToken)
    {
        var logValues = await container.GetLogsAsync(ct: cancellationToken);
        Console.WriteLine($"Stderr:{Environment.NewLine}{logValues.Stderr}");
        Console.WriteLine($"Stdout:{Environment.NewLine}{logValues.Stdout}");
        logValues.Stdout.Should().NotContain("warn:");
    }

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me")]
    private static string GenerateContainerImageTag() => $"system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
}
