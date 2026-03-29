using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IArticleMappingRepository
{
    Task SaveOrUpdateMappingAsync(OnlineArticleMapping mapping, CancellationToken cancellationToken = default);

    Task<OnlineArticleMapping> GetMappingAsync(string storeKey, string articleName, CancellationToken cancellationToken = default);

    Task<IEnumerable<OnlineArticleMapping>> GetAllMappingsAsync(string storeKey, string articleName, CancellationToken cancellationToken = default);
}
