using System.ComponentModel.DataAnnotations;

namespace ShoppingAgent.Options;

public class LlmClientOptions
{
    public const string SectionName = "LlmClient";

    [Required]
    [Url]
    public string Endpoint { get; set; } = "https://api.mistral.ai/v1";

    [Required]
    public string DefaultModel { get; set; } = "mistral-small-latest";

    [Required]
    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Mistral API key. Loaded from Docker Secret, environment variable, or appsettings.
    /// In production: mount as Docker Secret at /run/secrets/LlmClient__ApiKey or set env var LlmClient__ApiKey.
    /// In development: set in appsettings.Development.json (never commit).
    /// </summary>
    [Required]
    public string ApiKey { get; set; }
}
