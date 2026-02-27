using DTO.ArticleGroup;

namespace ServiceLayer;

public interface IArticleGroupService
{
    ExistingArticleGroupDto CreateArticleGroup(NewArticleGroupDto newArticleGroupDto);

    void DeleteArticleGroup(DeleteArticleGroupDto deleteArticleGroupDto);

    IEnumerable<ExistingArticleGroupDto> GetAllArticleGroups();
}
