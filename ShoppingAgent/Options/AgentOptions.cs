using System.ComponentModel.DataAnnotations;

namespace ShoppingAgent.Options;

public class AgentOptions
{
    public const string SectionName = "Agent";

    [Required]
    [Range(1, 500)]
    public int MaxToolCallingIterations { get; set; } = 50;

    [Required]
    [Range(1, 50)]
    public int ToolFailureThreshold { get; set; } = 3;
}
