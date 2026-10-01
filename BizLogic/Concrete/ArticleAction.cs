using BizDbAccess;
using DTO.Article;

namespace BizLogic.Concrete;

public class ArticleAction(IArticleDbAccess articleDbAccess) : IArticleAction
{
    /// <inheritdoc />
    public ExistingArticleDto CreateArticle(NewArticleDto newArticleDto)
    {
        var newArticle = newArticleDto.ToEntity();
        var createdArticle = articleDbAccess.AddArticle(newArticle);

        return createdArticle.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteArticleAsync(DeleteArticleDto deleteArticleDto, CancellationToken cancellationToken = default)
    {
        var article = await articleDbAccess.GetArticleAsync(deleteArticleDto.ArticleId, cancellationToken);
        articleDbAccess.DeleteArticle(article);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default)
        => (await articleDbAccess.GetArticlesAsync(cancellationToken)).Select(article => article.ToDto()).ToList();
}
