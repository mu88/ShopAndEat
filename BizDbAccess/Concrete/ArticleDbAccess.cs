using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class ArticleDbAccess(EfCoreContext context) : IArticleDbAccess
{
    public Article AddArticle(Article article) => context.Articles.Add(article).Entity;

    /// <inheritdoc />
    public void DeleteArticle(Article article) => context.Articles.Remove(article);

    /// <inheritdoc />
    public Task<Article> GetArticleAsync(ArticleId articleId, CancellationToken cancellationToken = default)
        => context.Articles.SingleAsync(article => article.ArticleId == articleId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Article>> GetArticlesAsync(CancellationToken cancellationToken = default)
        => await context.Articles.ToListAsync(cancellationToken);
}