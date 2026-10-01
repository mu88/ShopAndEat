using DTO.ArticleGroup;
using EfArticle = DataLayer.EfClasses.Article;

namespace DTO.Article;

public static class ArticleMapper
{
    public static ExistingArticleDto ToDto(this EfArticle entity)
        => new(entity.ArticleId, entity.Name, entity.ArticleGroup.ToDto(), entity.IsInventory);

    public static EfArticle ToEntity(this NewArticleDto dto)
        => new(dto.Name, dto.ArticleGroup.ToEntity(), dto.IsInventory);

    public static EfArticle ToEntity(this ExistingArticleDto dto)
        => new(dto.Name, dto.ArticleGroup.ToEntity(), dto.IsInventory);
}
