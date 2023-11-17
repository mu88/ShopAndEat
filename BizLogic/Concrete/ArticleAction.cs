using AutoMapper;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.Article;

namespace BizLogic.Concrete;

public class ArticleAction : IArticleAction
{
    public ArticleAction(IArticleDbAccess articleDbAccess, IMapper mapper)
    {
        ArticleDbAccess = articleDbAccess;
        Mapper = mapper;
    }

    private IArticleDbAccess ArticleDbAccess { get; }

    private IMapper Mapper { get; }

    /// <inheritdoc />
    public ExistingArticleDto CreateArticle(NewArticleDto newArticleDto)
    {
        var newArticle = Mapper.Map<Article>(newArticleDto);
        var createdArticle = ArticleDbAccess.AddArticle(newArticle);

        return Mapper.Map<ExistingArticleDto>(createdArticle);
    }

    /// <inheritdoc />
    public void DeleteArticle(DeleteArticleDto deleteArticleDto)
    {
        var article = ArticleDbAccess.GetArticle(deleteArticleDto.ArticleId);
        ArticleDbAccess.DeleteArticle(article);
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleDto> GetAllArticles()
    {
        return Mapper.Map<IEnumerable<ExistingArticleDto>>(ArticleDbAccess.GetArticles());
    }
}