using DTO.Article;
using DTO.Unit;
using EfPurchaseItem = DataLayer.EfClasses.PurchaseItem;

namespace DTO.PurchaseItem;

public static class PurchaseItemMapper
{
    public static ExistingPurchaseItemDto ToDto(this EfPurchaseItem entity)
        => new(entity.Article.ToDto(), entity.Unit.ToDto(), (uint)entity.Quantity, entity.PurchaseItemId);

    public static NewPurchaseItemDto ToNewDto(this EfPurchaseItem entity)
        => new(entity.Article.ToDto(), entity.Unit.ToDto(), entity.Quantity);

    public static EfPurchaseItem ToEntity(this NewPurchaseItemDto dto)
        => new(dto.Article.ToEntity(), dto.Quantity, dto.Unit.ToEntity());

    public static EfPurchaseItem ToEntity(this ExistingPurchaseItemDto dto)
        => new(dto.Article.ToEntity(), dto.Quantity, dto.Unit.ToEntity());
}
