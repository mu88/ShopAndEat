using DTO.Article;
using DTO.Unit;

namespace DTO.Ingredient;

public class NewIngredientDto(ExistingArticleDto article, double quantity, ExistingUnitDto unit)
{
    public ExistingArticleDto Article { get; } = article;

    public double Quantity { get; } = quantity;

    public ExistingUnitDto Unit { get; } = unit;
}