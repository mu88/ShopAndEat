using System.ComponentModel.DataAnnotations;
using ShoppingAgent.Models;

namespace ShoppingAgent.Options;

public class ShopOptions
{
    public const string SectionName = "Shops";

    [Required]
    [MinLength(1)]
    public List<ShopConfig> Shops { get; set; } = [];
}
