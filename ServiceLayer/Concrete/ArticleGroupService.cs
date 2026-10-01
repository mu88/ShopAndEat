using DataLayer.EfClasses;
using DTO.ArticleGroup;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class ArticleGroupService(SimpleCrudHelper simpleCrudHelper) : IArticleGroupService
{
    public async Task<ExistingArticleGroupDto> CreateArticleGroupAsync(NewArticleGroupDto newArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleGroupService.CreateArticleGroupAsync");
        return await simpleCrudHelper.CreateAsync<NewArticleGroupDto, ArticleGroup, ExistingArticleGroupDto>(newArticleGroupDto, dto => dto.ToEntity(), entity => entity.ToDto(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteArticleGroupAsync(DeleteArticleGroupDto deleteArticleGroupDto, CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleGroupService.DeleteArticleGroupAsync");
        await simpleCrudHelper.DeleteAsync<ArticleGroup>(deleteArticleGroupDto.ArticleGroupId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExistingArticleGroupDto>> GetAllArticleGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("ArticleGroupService.GetAllArticleGroupsAsync");
        return await simpleCrudHelper.GetAllAsDtoAsync<ArticleGroup, ExistingArticleGroupDto>(entity => entity.ToDto(), cancellationToken);
    }
}
