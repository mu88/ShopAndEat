using DTO.ArticleGroup;

namespace ServiceLayer;

public interface IArticleGroupService
{
    Task<ExistingArticleGroupDto> CreateArticleGroupAsync(NewArticleGroupDto newArticleGroupDto, CancellationToken cancellationToken = default);

    Task DeleteArticleGroupAsync(DeleteArticleGroupDto deleteArticleGroupDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingArticleGroupDto>> GetAllArticleGroupsAsync(CancellationToken cancellationToken = default);
}
