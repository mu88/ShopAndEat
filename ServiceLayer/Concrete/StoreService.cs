using DataLayer.EfClasses;
using DTO.Store;
using ServiceLayer.Diagnostics;

namespace ServiceLayer.Concrete;

public class StoreService(SimpleCrudHelper simpleCrudHelper) : IStoreService
{
    public async Task<IReadOnlyList<ExistingStoreDto>> GetAllStoresAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ServiceLayerDiagnostics.ActivitySource.StartActivity("StoreService.GetAllStoresAsync");
        return await simpleCrudHelper.GetAllAsDtoAsync<Store, ExistingStoreDto>(entity => entity.ToDto(), cancellationToken);
    }
}
