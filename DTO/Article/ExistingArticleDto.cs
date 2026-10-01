using DataLayer.EfClasses;
using DTO.ArticleGroup;

namespace DTO.Article;

public record ExistingArticleDto(
    ArticleId ArticleId,
    string Name,
    ExistingArticleGroupDto ArticleGroup,
    bool IsInventory);
