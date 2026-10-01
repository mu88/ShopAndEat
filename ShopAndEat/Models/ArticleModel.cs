using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class ArticleModel
{
    public ArticleModel()
    {
    }

    [Required]
    public int ArticleId { get; set; }

    [Required]
    public string ArticleName { get; set; } = string.Empty;

    [Required]
    public string ArticleGroupName { get; set; } = string.Empty;

    [Required]
    public bool IsInventory { get; set; }
}
