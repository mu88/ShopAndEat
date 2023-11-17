using DataLayer.EfClasses;
using DTO.Store;

namespace ServiceLayer.Concrete;

public class StoreService : IStoreService
{
    public StoreService(SimpleCrudHelper simpleCrudHelper)
    {
        SimpleCrudHelper = simpleCrudHelper;
    }

    private SimpleCrudHelper SimpleCrudHelper { get; }

    /// <inheritdoc />
    public IEnumerable<ExistingStoreDto> GetAllStores()
    {
        return SimpleCrudHelper.GetAllAsDto<Store, ExistingStoreDto>();
    }
}