using DataLayer.EfClasses;
using DTO.ArticleGroup;

namespace ServiceLayer.Concrete;

public class ArticleGroupService : IArticleGroupService
{
    public ArticleGroupService(SimpleCrudHelper simpleCrudHelper)
    {
        SimpleCrudHelper = simpleCrudHelper;
    }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    /// <inheritdoc />
    public ExistingArticleGroupDto CreateArticleGroup(NewArticleGroupDto newArticleGroupDto)
    {
        return SimpleCrudHelper.Create<NewArticleGroupDto, ArticleGroup, ExistingArticleGroupDto>(newArticleGroupDto);
    }

    /// <inheritdoc />
    public void DeleteArticleGroup(DeleteArticleGroupDto deleteArticleGroupDto)
    {
        SimpleCrudHelper.Delete<ArticleGroup>(deleteArticleGroupDto.ArticleGroupId);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleGroupDto> GetAllArticleGroups()
    {
        return SimpleCrudHelper.GetAllAsDto<ArticleGroup, ExistingArticleGroupDto>();
    }
}