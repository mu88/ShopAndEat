using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class ArticleMappingRepository(EfCoreContext context, TimeProvider timeProvider) : IArticleMappingRepository
{
    public async Task SaveOrUpdateMappingAsync(OnlineArticleMapping mapping, CancellationToken cancellationToken = default)
    {
        var existingMapping = await context.OnlineArticleMappings
            .FirstOrDefaultAsync(existing => existing.ArticleName == mapping.ArticleName
                                   && existing.StoreKey == mapping.StoreKey
                                   && existing.StoreProductCode == mapping.StoreProductCode, cancellationToken);

        if (existingMapping != null)
        {
            existingMapping.RecordMatch(mapping.StoreProductName, mapping.StoreProductPrice, mapping.Confidence, mapping.MatchMethod, mapping.QuantityPerUnit, timeProvider.GetUtcNow());
            existingMapping.RecordFeedback();
        }
        else
        {
            var now = timeProvider.GetUtcNow();
            var newMapping = new OnlineArticleMapping(mapping.ArticleName, mapping.StoreKey, mapping.StoreProductCode, now);
            newMapping.RecordMatch(mapping.StoreProductName, mapping.StoreProductPrice, mapping.Confidence, mapping.MatchMethod, mapping.QuantityPerUnit, now);
            context.OnlineArticleMappings.Add(newMapping);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OnlineArticleMapping?> GetMappingAsync(string storeKey, string articleName, CancellationToken cancellationToken = default)
    {
        return await context.OnlineArticleMappings
            .Where(mapping => mapping.StoreKey == storeKey && mapping.ArticleName == articleName)
            .OrderByDescending(mapping => mapping.FeedbackCount)
            .ThenByDescending(mapping => mapping.Confidence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<OnlineArticleMapping>> GetAllMappingsAsync(string storeKey, string articleName, CancellationToken cancellationToken = default)
    {
        return await context.OnlineArticleMappings
            .Where(mapping => mapping.StoreKey == storeKey && mapping.ArticleName == articleName)
            .OrderByDescending(mapping => mapping.FeedbackCount)
            .ThenByDescending(mapping => mapping.LastUsedAt)
            .ToListAsync(cancellationToken);
    }
}
