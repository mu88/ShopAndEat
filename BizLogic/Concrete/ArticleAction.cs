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
    public void DeleteArticle(DeleteArticleDto deleteArticleDto)
    {
        var article = articleDbAccess.GetArticle(deleteArticleDto.ArticleId);
        articleDbAccess.DeleteArticle(article);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleDto> GetAllArticles() => articleDbAccess.GetArticles().Select(a => a.ToDto());
}
