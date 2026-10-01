using DTO.Article;
using DTO.Unit;

namespace DTO.Ingredient;

public record NewIngredientDto(ExistingArticleDto Article, double Quantity, ExistingUnitDto Unit);
