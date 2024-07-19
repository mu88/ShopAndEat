using System.ComponentModel.DataAnnotations;

namespace ShopAndEat.Models;

public class RecipeModel
{
    [Required]
    public int RecipeId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public int NumberOfDays { get; set; }

    [Required]
    public int NumberOfPersons { get; set; }
}