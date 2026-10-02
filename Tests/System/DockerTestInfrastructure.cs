using System.Diagnostics.CodeAnalysis;
using CliWrap;
using CliWrap.Buffered;
using FluentAssertions;

namespace Tests.System;

internal static class DockerTestInfrastructure
{
    internal static async Task<bool> BuildDockerImageOfAppAsync(string containerImageTag, CancellationToken cancellationToken)
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
        Console.WriteLine(buildResult.StandardOutput);
        return buildResult.IsSuccess;
    }

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me")]
    internal static string GenerateContainerImageTag() => $"0.0.0-system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
}
