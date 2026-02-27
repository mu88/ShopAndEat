using DTO.Article;
using DTO.Unit;

namespace DTO.Ingredient;

public class ExistingIngredientDto(
    ExistingArticleDto article,
    double quantity,
    ExistingUnitDto unit,
    int ingredientId)
{
    public ExistingArticleDto Article { get; } = article;

    public double Quantity { get; } = quantity;

    public ExistingUnitDto Unit { get; } = unit;

    public int IngredientId { get; } = ingredientId;
}
