using Microsoft.Extensions.Configuration;

namespace ShopAndEat;

/// <summary>
/// Pure, unit-testable helper logic extracted from Program.cs's startup Docker-secrets loading, so the
/// file-exists/does-not-exist branches can be covered without depending on the real, container-only
/// <c>/run/secrets</c> path.
/// </summary>
internal static class DockerSecretsLoader
{
    public static void ApplyLlmApiKeySecret(IConfiguration configuration, string secretFilePath)
    {
        if (File.Exists(secretFilePath))
        {
            configuration["LlmClient:ApiKey"] = File.ReadAllText(secretFilePath).Trim();
        }
    }
}
