using DataLayer.EfClasses;
using DTO.ArticleGroup;

namespace ServiceLayer.Concrete;

public class ArticleGroupService(SimpleCrudHelper simpleCrudHelper) : IArticleGroupService
{
    public ExistingArticleGroupDto CreateArticleGroup(NewArticleGroupDto newArticleGroupDto)
        => simpleCrudHelper.Create<NewArticleGroupDto, ArticleGroup, ExistingArticleGroupDto>(newArticleGroupDto, dto => dto.ToEntity(), entity => entity.ToDto());

    /// <inheritdoc />
    public void DeleteArticleGroup(DeleteArticleGroupDto deleteArticleGroupDto) => simpleCrudHelper.Delete<ArticleGroup>(deleteArticleGroupDto.ArticleGroupId);

    /// <inheritdoc />
    public IEnumerable<ExistingArticleGroupDto> GetAllArticleGroups() => simpleCrudHelper.GetAllAsDto<ArticleGroup, ExistingArticleGroupDto>(entity => entity.ToDto());
}
