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
    public string ArticleName { get; set; }

    [Required]
    public string ArticleGroupName { get; set; }

    [Required]
    public bool IsInventory { get; set; }
}
