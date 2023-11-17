using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IArticleDbAccess
{
    Article AddArticle(Article article);

    void DeleteArticle(Article article);

    Article GetArticle(int articleId);

    IEnumerable<Article> GetArticles();
}