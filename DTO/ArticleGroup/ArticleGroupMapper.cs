using EfArticleGroup = DataLayer.EfClasses.ArticleGroup;

namespace DTO.ArticleGroup;

public static class ArticleGroupMapper
{
    public static ExistingArticleGroupDto ToDto(this EfArticleGroup entity)
        => new(entity.ArticleGroupId, entity.Name);

    public static EfArticleGroup ToEntity(this NewArticleGroupDto dto)
        => new(dto.Name);

    public static EfArticleGroup ToEntity(this ExistingArticleGroupDto dto)
        => new(dto.Name);
}
