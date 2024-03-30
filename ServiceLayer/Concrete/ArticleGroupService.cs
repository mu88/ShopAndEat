using DataLayer.EfClasses;
using DTO.ArticleGroup;

namespace ServiceLayer.Concrete;

public class ArticleGroupService(SimpleCrudHelper simpleCrudHelper) : IArticleGroupService
{
    public ExistingArticleGroupDto CreateArticleGroup(NewArticleGroupDto newArticleGroupDto)
    {
        return simpleCrudHelper.Create<NewArticleGroupDto, ArticleGroup, ExistingArticleGroupDto>(newArticleGroupDto);
    }

    /// <inheritdoc />
    public void DeleteArticleGroup(DeleteArticleGroupDto deleteArticleGroupDto)
    {
        simpleCrudHelper.Delete<ArticleGroup>(deleteArticleGroupDto.ArticleGroupId);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleGroupDto> GetAllArticleGroups()
    {
        return simpleCrudHelper.GetAllAsDto<ArticleGroup, ExistingArticleGroupDto>();
    }
}