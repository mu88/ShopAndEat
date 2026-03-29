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
}
