using DTO.Article;

namespace BizLogic;

public interface IArticleAction
{
    ExistingArticleDto CreateArticle(NewArticleDto newArticleDto);

    Task DeleteArticleAsync(DeleteArticleDto deleteArticleDto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExistingArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default);
}
