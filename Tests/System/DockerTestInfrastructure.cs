using System.Diagnostics.CodeAnalysis;
using CliWrap;
using CliWrap.Buffered;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;

namespace Tests.System;

internal static class DockerTestInfrastructure
{
    internal static async Task BuildDockerImageOfAppAsync(string containerImageTag, CancellationToken cancellationToken)
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

    internal static async Task<IContainer> StartAppInContainersAsync(string containerImageTag, CancellationToken cancellationToken)
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

    internal static Uri GetAppBaseAddress(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}/shopAndEat");

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me")]
    internal static string GenerateContainerImageTag() => $"system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

    private static IContainer BuildAppContainer(INetwork network, string containerImageTag)
        => new ContainerBuilder($"shopandeat:{containerImageTag}-chiseled")
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Content root path: /app",
                    strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))) // as it's a chiseled container, waiting for the port does not work
            .Build();
}
