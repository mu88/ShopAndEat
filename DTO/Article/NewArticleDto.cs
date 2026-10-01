using DTO.ArticleGroup;

namespace DTO.Article;

public record NewArticleDto(string Name, ExistingArticleGroupDto ArticleGroup, bool IsInventory);
