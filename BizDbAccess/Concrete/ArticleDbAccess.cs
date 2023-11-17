using DataLayer.EF;
using DataLayer.EfClasses;

namespace BizDbAccess.Concrete;

public class ArticleDbAccess : IArticleDbAccess
{
    public ArticleDbAccess(EfCoreContext context)
    {
        Context = context;
    }

    private EfCoreContext Context { get; }

    /// <inheritdoc />
    public Article AddArticle(Article article)
    {
        return Context.Articles.Add(article).Entity;
    }

    /// <inheritdoc />
    public void DeleteArticle(Article article)
    {
        Context.Articles.Remove(article);
    }

    /// <inheritdoc />
    public Article GetArticle(int articleId)
    {
        return Context.Articles.Single(x => x.ArticleId == articleId);
    }

    /// <inheritdoc />
    public IEnumerable<Article> GetArticles()
    {
        return Context.Articles;
    }
}