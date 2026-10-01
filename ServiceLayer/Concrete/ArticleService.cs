using BizLogic;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Article;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class ArticleService(
    IArticleAction articleAction,
    EfCoreContext context,
    SimpleCrudHelper simpleCrudHelper)
    : IArticleService
{
    public async Task<ExistingArticleDto> CreateArticleAsync(NewArticleDto newArticleDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleService.CreateArticleAsync");

        // No ToEntity() on NewArticleDto: creating an Article requires resolving the referenced
        // ArticleGroup by ID against the database, which a pure mapper cannot do without a DbContext.
        var articleGroup = await simpleCrudHelper.FindAsync<ArticleGroup>(newArticleDto.ArticleGroup.ArticleGroupId, cancellationToken);
        var newArticle = new Article(newArticleDto.Name, articleGroup, isInventory: newArticleDto.IsInventory);
        var createdArticle = context.Articles.Add(newArticle);
        await context.SaveChangesAsync(cancellationToken);

        return createdArticle.Entity.ToDto();
    }

    public async Task DeleteArticleAsync(DeleteArticleDto deleteArticleDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleService.DeleteArticleAsync");
        await articleAction.DeleteArticleAsync(deleteArticleDto, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleService.GetAllArticlesAsync");
        return (await articleAction.GetAllArticlesAsync(cancellationToken)).OrderBy(article => article.Name, StringComparer.Ordinal).ToList();
    }

    public async Task UpdateArticleAsync(ExistingArticleDto existingArticleDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleService.UpdateArticleAsync");
        var articleGroup = await simpleCrudHelper.FindAsync<ArticleGroup>(existingArticleDto.ArticleGroup.ArticleGroupId, cancellationToken);
        var article = await simpleCrudHelper.FindAsync<Article>(existingArticleDto.ArticleId, cancellationToken);
        article.Update(existingArticleDto.Name, articleGroup, existingArticleDto.IsInventory);
        await context.SaveChangesAsync(cancellationToken);
    }
}
