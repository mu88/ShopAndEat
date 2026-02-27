using AutoMapper;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.Article;

namespace BizLogic.Concrete;

public class ArticleAction(IArticleDbAccess articleDbAccess, IMapper mapper) : IArticleAction
{
    /// <inheritdoc />
    public ExistingArticleDto CreateArticle(NewArticleDto newArticleDto)
    {
        var newArticle = mapper.Map<Article>(newArticleDto);
        var createdArticle = articleDbAccess.AddArticle(newArticle);

        return mapper.Map<ExistingArticleDto>(createdArticle);
    }

    /// <inheritdoc />
    public void DeleteArticle(DeleteArticleDto deleteArticleDto)
    {
        var article = articleDbAccess.GetArticle(deleteArticleDto.ArticleId);
        articleDbAccess.DeleteArticle(article);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleDto> GetAllArticles() => mapper.Map<IEnumerable<ExistingArticleDto>>(articleDbAccess.GetArticles());
}
