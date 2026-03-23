using EfStore = DataLayer.EfClasses.Store;

namespace DTO.Store;

public static class StoreMapper
{
    public static ExistingStoreDto ToDto(this EfStore entity)
        => new(entity.StoreId, entity.Name);
}
