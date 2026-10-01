using DTO.Article;

namespace ServiceLayer;

public interface IArticleService
{
    Task<ExistingArticleDto> CreateArticleAsync(NewArticleDto newArticleDto, CancellationToken cancellationToken = default);

    Task DeleteArticleAsync(DeleteArticleDto deleteArticleDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default);

    Task UpdateArticleAsync(ExistingArticleDto existingArticleDto, CancellationToken cancellationToken = default);
}
