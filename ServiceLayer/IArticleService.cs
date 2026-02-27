using DTO.Article;

namespace ServiceLayer;

public interface IArticleService
{
    ExistingArticleDto CreateArticle(NewArticleDto newArticleDto);

    void DeleteArticle(DeleteArticleDto deleteArticleDto);

    IEnumerable<ExistingArticleDto> GetAllArticles();

    void UpdateArticle(ExistingArticleDto existingArticleDto);
}
