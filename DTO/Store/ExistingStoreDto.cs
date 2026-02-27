namespace DTO.Store;

public class ExistingStoreDto(int storeId, string name)
{
    public int StoreId { get; } = storeId;

    public string Name { get; } = name;
}
