using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IArticleDbAccess
{
    Article AddArticle(Article article);

    void DeleteArticle(Article article);

    Task<Article> GetArticleAsync(ArticleId articleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Article>> GetArticlesAsync(CancellationToken cancellationToken = default);
}