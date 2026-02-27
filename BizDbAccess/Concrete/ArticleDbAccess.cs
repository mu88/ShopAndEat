using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class ArticleDbAccess(EfCoreContext context) : IArticleDbAccess
{
    public Article AddArticle(Article article) => context.Articles.Add(article).Entity;

    /// <inheritdoc />
    public void DeleteArticle(Article article) => context.Articles.Remove(article);

    /// <inheritdoc />
    public Article GetArticle(int articleId) => context.Articles.Single(x => x.ArticleId == articleId);

    /// <inheritdoc />
    public IEnumerable<Article> GetArticles() => context.Articles;
}
