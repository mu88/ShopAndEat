using System.ComponentModel.DataAnnotations;

namespace ShoppingAgent.Options;

public class ExtensionOptions
{
    public const string SectionName = "Extension";

    [Required]
    [Range(1, 300)]
    public int ToolCallTimeoutSeconds { get; set; } = 30;
}
