using DTO.ArticleGroup;
using EfArticle = DataLayer.EfClasses.Article;

namespace DTO.Article;

public static class ArticleMapper
{
    public static ExistingArticleDto ToDto(this EfArticle entity)
        => new(entity.ArticleId, entity.Name, entity.ArticleGroup.ToDto(), entity.IsInventory);

    public static EfArticle ToEntity(this NewArticleDto dto)
        => new() { Name = dto.Name, ArticleGroup = dto.ArticleGroup.ToEntity(), IsInventory = dto.IsInventory };

    public static EfArticle ToEntity(this ExistingArticleDto dto)
        => new() { Name = dto.Name, ArticleGroup = dto.ArticleGroup.ToEntity(), IsInventory = dto.IsInventory };
}
