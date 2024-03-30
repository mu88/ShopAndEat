using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class ArticleDbAccess(EfCoreContext context) : IArticleDbAccess
{
    public Article AddArticle(Article article)
    {
        return context.Articles.Add(article).Entity;
    }

    /// <inheritdoc />
    public void DeleteArticle(Article article)
    {
        context.Articles.Remove(article);
    }

    /// <inheritdoc />
    public Article GetArticle(int articleId)
    {
        return context.Articles.Single(x => x.ArticleId == articleId);
    }

    /// <inheritdoc />
    public IEnumerable<Article> GetArticles()
    {
        return context.Articles;
    }
}