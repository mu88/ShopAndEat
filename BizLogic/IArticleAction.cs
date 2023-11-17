using DTO.Article;

namespace BizLogic;

public interface IArticleAction
{
    ExistingArticleDto CreateArticle(NewArticleDto newArticleDto);

    void DeleteArticle(DeleteArticleDto deleteArticleDto);

    IEnumerable<ExistingArticleDto> GetAllArticles();
}