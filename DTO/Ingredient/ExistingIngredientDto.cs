using DTO.Article;
using DTO.Unit;

namespace DTO.Ingredient;

public record ExistingIngredientDto(
    ExistingArticleDto Article,
    double Quantity,
    ExistingUnitDto Unit,
    int IngredientId);
