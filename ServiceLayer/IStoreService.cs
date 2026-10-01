using DTO.Store;

namespace ServiceLayer;

public interface IStoreService
{
    Task<IReadOnlyList<ExistingStoreDto>> GetAllStoresAsync(CancellationToken cancellationToken = default);
}
