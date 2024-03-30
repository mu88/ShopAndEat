using DataLayer.EfClasses;
using DTO.Store;

namespace ServiceLayer.Concrete;

public class StoreService(SimpleCrudHelper simpleCrudHelper) : IStoreService
{
    public IEnumerable<ExistingStoreDto> GetAllStores()
    {
        return simpleCrudHelper.GetAllAsDto<Store, ExistingStoreDto>();
    }
}