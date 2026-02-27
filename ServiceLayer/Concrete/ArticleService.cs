using AutoMapper;
using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;

namespace ServiceLayer.Concrete;

public class ArticleService(
    IArticleAction articleAction,
    EfCoreContext context,
    IMapper mapper,
    SimpleCrudHelper simpleCrudHelper)
    : IArticleService
{
    public ExistingArticleDto CreateArticle(NewArticleDto newArticleDto)
    {
        // TODO mu88: Try to avoid this manual mapping logic
        var articleGroup = simpleCrudHelper.Find<ArticleGroup>(newArticleDto.ArticleGroup.ArticleGroupId);
        var newArticle = new Article { Name = newArticleDto.Name, ArticleGroup = articleGroup, IsInventory = newArticleDto.IsInventory };
        var createdArticle = context.Articles.Add(newArticle);
        context.SaveChanges();

        return mapper.Map<ExistingArticleDto>(createdArticle.Entity);
    }

    public void DeleteArticle(DeleteArticleDto deleteArticleDto)
    {
        articleAction.DeleteArticle(deleteArticleDto);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public IEnumerable<ExistingArticleDto> GetAllArticles() => articleAction.GetAllArticles().OrderBy(x => x.Name, StringComparer.Ordinal);

    public void UpdateArticle(ExistingArticleDto existingArticleDto)
    {
        var articleGroup = simpleCrudHelper.Find<ArticleGroup>(existingArticleDto.ArticleGroup.ArticleGroupId);
        var article = simpleCrudHelper.Find<Article>(existingArticleDto.ArticleId);
        article.ArticleGroup = articleGroup;
        article.IsInventory = existingArticleDto.IsInventory;
        article.Name = existingArticleDto.Name;
        context.SaveChanges();
    }
}
